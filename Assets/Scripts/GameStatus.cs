using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System; 
using System.Text; 
using System.IO; 
using System.Xml; 
using System.Xml.Serialization; 
using wox.serial;

//このクラスをサーバーとクライアントで共有するイメージ。
//サーバー側では常にバックアップを取って行く。
public class GameStatus {
    ////////////////////////////////////////////////////////////////////
    //Singleton
    ////////////////////////////////////////////////////////////////////
    private static GameStatus instance = null;
    public static GameStatus Instance {
        get {
            if( instance == null ) instance = new GameStatus();
            return instance;
        }
    }

    ////////////////////////////////////////////////////////////////////
    //Constants
    ////////////////////////////////////////////////////////////////////
    //system status
    public enum TRANSACTION { NONE, 
	    WAIT_FOR_RESPONSE,	//waiting...
	    RESPONSE_SUCCESS,	//succesfully processed.
	    RESPONSE_ERROR,	//something was wrong. reference to GameStatus.message.
	    MAX };
    public enum PHASE { 
	INIT,			//initialize.
	    LOGIN,		//connect to server and new game starts automatically. TRANSACTION
	    NEW_GAME,		//clear digits and history... TRANSACTION
	    SELECT,		//selecting bet and 4 digits and push buttos.
	    TRY,		//exec button pushed. waiting for server response. TRANSACTION
	    GIVE_UP,		//to new game starts.
	    RESULT,		//result for each try. 
	    FINALE,		//result for whole game. consumed maxTries times.
	    WIN,		//WIN.
	    ERROR,		//showing error dialog.
	    LOGOUT,		//disconnect from server. closing page or quitting web browser or whatever.
	    MAX };

    //max tries
    public readonly int maxTries = 7;

    //max digits
    public readonly int maxDigits = 4;

    //bets
    public readonly int[] bets = {10,20,30,40,50,60,70,80,90,100};

    //version
    public readonly string version = "0.0";

    //common data for both client and server.
    //Need to care about the coherence for yourself.
    [System.Serializable] [XmlRoot("commonData")] public class CommonData{
	////////////////////////////////////////////////////////////////////
	//version for CommonData.
	////////////////////////////////////////////////////////////////////
	public string version = "-";

	////////////////////////////////////////////////////////////////////
	//User Info
	////////////////////////////////////////////////////////////////////
	//user ID.optional.
	public string userID = "-";

	//seq and token. 
	public string seq = "-";
	public string token = "-";

	//creditBalance
	public int creditBalance;

	//timestamp
	public string timeStamp = "-";


	////////////////////////////////////////////////////////////////////
	//Game Info
	////////////////////////////////////////////////////////////////////
	public PHASE phase = PHASE.INIT;
	public PHASE oldPhase = PHASE.INIT;

	//game no since login
	public int gameCount = -1;

	//try no since new game
	public int tryCount = 0;

	//input numbers
	//public int[,] digits = null;
	//public int[][] digits = null;
	public List<int> digits = new List<int>();

	//cracked numbers
	//public int[] crackedDigits = null;
	public List<int> crackedDigits = new List<int>();

	//bet
	public int bet;

	//reward
	public int reward;

	//auto or manual
	public bool isAuto = false;

    }

    private CommonData data = null;

    //index for bet
    private int betID = 0;

    //cursor pos
    private int cursorPos = 0;

    //game api
    HttpUtility http = null;
#if UNITY_EDITOR
    string apiURL = "localhost:8888/GameAPI";

    public class RNG {
	static readonly float denom = 1000;
	static readonly float [] numerators = {
	    0.100f,		//初回の確率
	    0.111f,
	    0.125f,
	    0.142f,
	    0.166f,
	    0.200f,
	    0.250f,
	    0.333f,
	    0.500f,
	    1.0f
	};
	static readonly int [] odds = {
	    9999,		//初回の倍率
	    1000,
	    350,
	    150,
	    70,
	    40,
	    25,
	    15,
	    10,
	    5
	};

	static public bool IsHit( int counterNo, int tryNo ){
	    float thr = numerators[ tryNo ] * denom;
	    float val = UnityEngine.Random.value * denom;
	    return (bool)( thr >= val );
	}

	static public int GetOdds( int tryNo ){
	    return odds[ tryNo ];
	}
    }

#else
    string apiURL = "/GameAPI";
#endif

    ////////////////////////////////////////////////////////////////////
    //TRANSACTION
    ////////////////////////////////////////////////////////////////////
    private TRANSACTION transaction = TRANSACTION.NONE;
    public int phaseCounter;
    public int phaseStatus;

    private GameStatus(){
	NewCommonData();
    }

    ////////////////////////////////////////////////////////////////////
    //Update
    ////////////////////////////////////////////////////////////////////
    public void Update(){
	++phaseCounter;

	if( IsTransactionWait() ){
	    if( http.IsDone() ){
		if( http.IsSuccess() ){
		    SetTransactionDone();
		}else{
		    SetTransactionError();
		}
	    }
	}
    }


    ////////////////////////////////////////////////////////////////////
    //Utility
    ////////////////////////////////////////////////////////////////////
    public void SetHttpUtility( HttpUtility h ){
	http = h;
    }
    //New common data
    private void NewDigits(){
 	for( int i=0; i<maxTries; ++i ){
	    for( int j=0; j<maxDigits; ++j ) data.digits.Add( -1 );
 	}

	for( int i=0; i<maxDigits; ++ i ){
	    data.crackedDigits.Add(-1);
	}
	
// 	data.digits = new int[maxTries][];
// 	for( int i=0; i<maxTries; ++i ){
// 	    data.digits[i] = new int[maxDigits];
// 	}
// 	data.crackedDigits = new int[ maxDigits ];
    }
    private void NewCommonData(){
	data = new CommonData();
	data.version = version;
	NewDigits();
	ChangePhase( PHASE.INIT );
	ResetBet();
	ResetDigitsAll();
    }

    //time stamp for transaction
    public void SetTimeStamp(){
	data.timeStamp = System.DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    //Try Control
    public bool IsFinalTry( ){
	return data.tryCount == (maxTries-1);
    }
    public int TryCount( ){
	return data.tryCount;
    }

    //win query
    public bool IsWin(){
	for( int i=0; i<maxDigits; ++i ){
	    if( data.crackedDigits[i] < 0 ) return false;
	}
	return true;
    }

    //Credit contrl
    public int CreditBalance(){
	return data.creditBalance;
    }

    //Reward
    public int Reward(){
	return data.reward;
    }
    public void CalcReward(){
	data.reward = Bet() * RNG.GetOdds( TryCount() );
    }

    //Bet control
    public bool IsBetAble(){
	return ( data.phase == PHASE.SELECT && data.tryCount == 0 );
    }
    public int Bet(){
	return data.bet;
    }
    public bool UpBet(){
	if( IsBetAble() && (bets.Length-1) > betID ){
	    data.bet = bets[ ++betID ];
	    return true;
	}else{
	    return false;
	}
    }
    public bool DownBet(){
	if( IsBetAble() && betID > 0 ){
	    data.bet = bets[ --betID ];
	    return true;
	}else{
	    return false;
	}
    }
    public void ResetBet(){
	betID = 0;
	data.bet = bets[betID];
    }

    //Phase control
    public void ChangePhase( PHASE p ){
	data.oldPhase = data.phase;
	data.phase = p;
	phaseCounter = 0;
	phaseStatus = 0;
	Debug.Log( "Change Phase From: " + data.oldPhase + " To: " + data.phase );
    }
    public bool IsPhase( PHASE p ){
	return data.phase == p;
    }
    public PHASE Phase(  ){
	return data.phase;
    }

    //Digits control
    public void ExecAuto(){
	for( int i=0; i<maxDigits; ++i ){
	    if( data.crackedDigits[ i ] < 0 ){
		List<int> candidates = GetCandidates( i );
		int num = candidates[ (int)UnityEngine.Random.Range(0, candidates.Count-1) ];
		SetDigitAt( i, num );
	    }
	}
	cursorPos = maxDigits;
    }


    public bool IsDigitsFull(){
	return cursorPos >= maxDigits;
    }
    public bool IsValidDigit( int d ){
	return ( d >=0 && d < 10 );
    }
    public int GetCrackedDigitsCount(){
	int ret = 0;
	for( int i=0; i<maxDigits; ++i ){
	    if( data.crackedDigits[ i ] >= 0 ) ++ret;
	}
	return ret;
    }

    public int Index( int i, int j ){ 
	return i*maxDigits + j;
    }
    public void CrackDigitAt( int c ){
	if( !IsValidCursor(c) ) return;
	if( !IsValidTry(TryCount()) ) return;
	data.crackedDigits[ c ] = GetDigitAt( c );
    }

    public int GetDigitAt( int c ){
	return GetDigitAt( TryCount(), c );
    }
    public int GetDigitAt( int t, int c ){
	if( !IsValidCursor(c) ) return -1;
	if( !IsValidTry(t) ) return -1;
	return data.digits[ Index(t, c) ];
    }

    public void SetDigitAt( int t, int c, int d ){
	if( !IsValidCursor(c) ) return;
	if( !IsValidTry(t) ) return;
	if( !IsValidDigit(d) ) return;
	data.digits[ Index(t , c) ] = d;
    }
    public void SetDigitAt( int c, int d ){
	SetDigitAt( TryCount(), c, d );
    }
    public void ClearDigitAt( int t, int c ){
	if( !IsValidCursor(c) ) return;
	if( !IsValidTry(t) ) return;
	data.digits[ Index(t , c) ] = -1;
    }
    public void ClearDigitAt( int c ){
	ClearDigitAt( TryCount(), c );
    }

    //returns -1 if it's not cracked yet
    public int GetCrackedDigitAt( int c ){
	if( !IsValidCursor(c) ) return -1;
	return data.crackedDigits[ c ];
    }

    public bool IsCrackedDigitAt( int c ){
	return GetCrackedDigitAt(c) >= 0;
    }

    public bool IsUniqueDigitAt( int c ){
	if( TryCount() == 0 ) return true;

	int cur = GetDigitAt( c );
	for( int i=0; i<TryCount(); ++i ){
	    if( GetDigitAt( i, c ) == cur ) return false;
	}
	return true;
    }

    public List<int> GetCandidates( int cursor ){
	List<int> c = new List<int>();
	if( TryCount() == 0 ){
	    c.Add(0);	    c.Add(1);
	    c.Add(2);	    c.Add(3);
	    c.Add(4);	    c.Add(5);
	    c.Add(6);	    c.Add(7);
	    c.Add(8);	    c.Add(9);
	    return c;
	}
	
	for( int n=0; n<10; ++n ){
	    bool flg = true;
	    for( int t=0; flg && t<TryCount(); ++t ){
		if( n == GetDigitAt( t, cursor ) ) flg = false;
	    }
	    if( flg ) c.Add( n );
	    
	}
	return c;
    }

    public void ResetDigitsAll(){
	//clear history
	for( int i=0; i<maxTries; ++i ){
	    for( int j=0; j<maxDigits; ++j ){
		ClearDigitAt(i,j);
	    }
	}
	//clear crackedDigits
	for( int i=0; i<maxDigits; ++i ) data.crackedDigits[i] = -1;
    }

    //Try Control.
    public bool IsValidTry( int no ){
	return (no >= 0 && no < maxTries );
    }
    public void StartNextTry(){
	if( data.tryCount < (maxTries-1) ){
	    ++data.tryCount;
	    for( int i=0; i<maxDigits; ++i ){
		if( IsCrackedDigitAt(i) ){
		    SetDigitAt( i, GetCrackedDigitAt(i) );
		}
	    }
	    ResetCursor();
	}
    }

    //Cursor control
    public bool IsValidCursor( int no ){
	return ( no >= 0 && no < maxDigits );
    }

    public int Cursor(){
	return cursorPos;
    }

    public void ResetCursor(){
	cursorPos = -1;
	IncCursor();
    }

    public void IncCursor(){
	if( cursorPos >= maxDigits ) return;
	for( ++cursorPos; cursorPos<maxDigits; ++cursorPos ){
	    if( !IsCrackedDigitAt( cursorPos ) ) break;
	}
    }

    public void DecCursor(){
	if( cursorPos <= 0 ) return;
	for( --cursorPos; cursorPos>=0; --cursorPos ){
	    if( !IsCrackedDigitAt( cursorPos ) ) break;
	}
    }

    ////////////////////////////////////////////////////////////////////
    //LOGIN Transaction
    ////////////////////////////////////////////////////////////////////
    static readonly string[] dummyNames = {
	"jimmy", "jack", "winston", "sarah", "george"
    };
    public bool LoginTransaction(){
	//@todo dummy;
	//多分、httpのヘッダーかなんかにuserIDとseqが入っている。
	//これを送るとtokenとcreditBalanceが帰る。
	data.userID = dummyNames[ (int)(UnityEngine.Random.Range(0,4.9f)) ];
	data.seq = "poaweoi239d0934";
#if UNITY_EDITOR
	data.creditBalance = (int)(UnityEngine.Random.Range(300,999));
	SetTransactionDone();
	return true;
#else
	http.SendRequest( apiURL, SerializeCommonData() );
	return SetTransaction();
#endif
    }
    public void LoginTransactionDone(){
#if UNITY_EDITOR
#else
 	CommonData cd = DeserializeCommonData( http.GetResponseText() );
	data = null;
	data = cd;
#endif
	AdmitTransaction();
    }

    ////////////////////////////////////////////////////////////////////
    //NEW GAME Transaction
    ////////////////////////////////////////////////////////////////////
    public bool NewGameTransaction(){
	//@todo dummy;
	ResetDigitsAll();
	data.tryCount = 0;
	data.gameCount += 1;
	ResetCursor();
#if UNITY_EDITOR
	SetTransactionDone();
	return true;
#else
	return SetTransaction();
#endif
    }
    public void IncTryCount(){
	++data.tryCount;
    }

    ////////////////////////////////////////////////////////////////////
    //TRY Transaction
    ////////////////////////////////////////////////////////////////////
    public bool TryTransaction(){
#if UNITY_EDITOR
	for( int i=0; i<maxDigits; ++i ){
	    Debug.Log( "TRY " + i );
	    if( IsCrackedDigitAt( i ) ){
		//あたってる。
	    }else if( IsUniqueDigitAt( i ) && RNG.IsHit( i, maxTries - TryCount() ) ){
		CrackDigitAt( i );
		Debug.Log( "HERE " + i );
	    }
	}
	data.creditBalance -= Bet();
	if( data.creditBalance < 0 ){ data.creditBalance = 0; }
	SetTransactionDone();
	return true;
#else
	return SetTransaction();
#endif
    }
    public void TryTransactionDone(){
#if UNITY_EDITOR
#else
	CommonData cd = DeserializeCommonData( http.GetResponseText() );
	data = null;
	data = cd;
#endif
	AdmitTransaction();
    }

    ////////////////////////////////////////////////////////////////////
    //SELECT
    ////////////////////////////////////////////////////////////////////
    public bool SetCurrentDigit( int no ){
	if( !IsPhase( PHASE.SELECT ) ) return false;
	if( IsDigitsFull() ) return false;
	if( !IsValidDigit( no ) ) return false;
	if( !IsValidCursor(cursorPos) ) return false;
	if( !IsValidTry(data.tryCount) ) return false;
	SetDigitAt( data.tryCount, cursorPos, no );
	IncCursor();
	return true;
    }
    public bool ClearCurrentDigit(){
	if( !IsPhase( PHASE.SELECT ) ) return false;
	if( !IsValidTry( TryCount() ) ) return false;
	//if( !IsValidCursor( cursorPos ) ) return false;
	if( cursorPos == 0 && GetDigitAt( cursorPos )<0 ) return false;

	DecCursor();
	ClearDigitAt( cursorPos );
	return true;
    }
    public bool GiveUp(){
	if( !IsPhase( PHASE.SELECT ) ) return false;
	NewGameTransaction();
	return true;
    }


    ////////////////////////////////////////////////////////////////////
    //TRANSACTION control
    ////////////////////////////////////////////////////////////////////
    public void AdmitTransaction(){
	transaction = TRANSACTION.NONE;
    }
    public bool IsTransactionSuccess(){
	return transaction == TRANSACTION.RESPONSE_SUCCESS;
    }
    public bool IsTransactionFailure(){
	return transaction == TRANSACTION.RESPONSE_ERROR;
    }
    public bool IsTransactionWait(){
	return transaction == TRANSACTION.WAIT_FOR_RESPONSE;
    }
    public bool IsTransactionNone(){
	return transaction == TRANSACTION.NONE;
    }
    public void SetTransactionDone(){
	transaction = TRANSACTION.RESPONSE_SUCCESS;
    }
    public void SetTransactionError(){
	transaction = TRANSACTION.RESPONSE_SUCCESS;
    }
    public bool SetTransaction(){
	if( IsTransactionNone() ){
	    SetTimeStamp();
	    http.SendRequest( apiURL, SerializeCommonData() );
	    transaction = TRANSACTION.WAIT_FOR_RESPONSE;
	    return true;
	}else{
	    return false;
	}
    }

    ////////////////////////////////////////////////////////////////////
    //Serialize and Deserialize
    ////////////////////////////////////////////////////////////////////
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
    public string SerializeCommonData(){
 	using( StringWriter textWriter = new Utf8StringWriter() )
 	    {
 		var serializer = new XmlSerializer(typeof( CommonData ));
 		serializer.Serialize(textWriter, data);
 		return textWriter.ToString();
 	    }
    }

    void ParseString( XmlNode node, string tag, out string val ){
	//Debug.Log( tag + " " + node.SelectSingleNode(tag).InnerText );
	val = node.SelectSingleNode(tag).InnerText;
    }
    void ParseInt( XmlNode node, string tag, out int val ){
	//Debug.Log( tag + " " + node.SelectSingleNode(tag).InnerText );
	val = int.Parse( node.SelectSingleNode(tag).InnerText );
    }
    void ParseBool( XmlNode node, string tag, out bool val ){
	//Debug.Log( tag + " " + node.SelectSingleNode(tag).InnerText );
	val = bool.Parse( node.SelectSingleNode(tag).InnerText );
    }
    void ParsePHASE( XmlNode node, string tag, out PHASE val ){
	//Debug.Log( tag + " " + node.SelectSingleNode(tag).InnerText );
	val = (PHASE)Enum.Parse( typeof(PHASE), node.SelectSingleNode(tag).InnerText );
    }
    void ParseInts( XmlDocument xmlDoc, string tag, out List<int> lst ){
	lst = new List<int>();
        XmlNodeList nl = xmlDoc.GetElementsByTagName(tag);
	foreach( XmlNode n in nl ){
	    //Debug.Log( tag + " " + n.InnerText );
	    lst.Add( int.Parse(n.InnerText) );
	}
    }
	
    public CommonData DeserializeCommonData( string xmlString ){
	CommonData cd = new CommonData();
	XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(new StringReader(xmlString));

        XmlNodeList tt = xmlDoc.GetElementsByTagName("commonData");
 	XmlNode root = tt[0];
	ParseString( root, "version", out cd.version );
	ParseString( root, "userID", out cd.userID );
	ParseString( root, "seq", out cd.seq );
	ParseString( root, "token", out cd.token );
	ParseInt( root, "creditBalance", out cd.creditBalance );
	ParseString( root, "timeStamp", out cd.timeStamp );
	ParsePHASE( root, "phase", out cd.phase );
	ParsePHASE( root, "oldPhase", out cd.oldPhase );
	ParseInt( root, "gameCount", out cd.gameCount );
	ParseInt( root, "tryCount", out cd.tryCount );
	ParseInt( root, "bet", out cd.bet );
	ParseInt( root, "reward", out cd.reward );
	ParseBool( root, "isAuto", out cd.isAuto );
 	ParseInts( xmlDoc, "digits", out cd.digits );
 	ParseInts( xmlDoc, "crackedDigits", out cd.crackedDigits );

	return cd;

//         XmlNode talk0 = talkList[0];
//         XmlNode talk1 = talkList[1];

//         XmlNodeList speakList0 = talk0.ChildNodes;
//         XmlNodeList speakList1 = talk1.ChildNodes;

//         Debug.Log( root.Name ); // talks

//         Debug.Log( talk0.Attributes["person"].Value ); // 2
//         Debug.Log( talk1.Attributes["person"].Value ); // 1

//         Debug.Log( speakList0[0].Attributes["content"].Value ); // こんにちは
//         Debug.Log( speakList0[1].Attributes["content"].Value ); // ありがとう
//         Debug.Log( speakList0[2].Attributes["content"].Value 

// 	var serializer = new XmlSerializer(typeof( CommonData ));
// 	return serializer.Deserialize(new StringReader(xmlString)) as CommonData;
    }
	
    

}
