using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Game  : MonoBehaviour {
    private GameStatus status = GameStatus.Instance;
    private bool drawReq = true;

    private readonly string YouFailedString = "You Failed...";
    private readonly string YouWinString = "You Won!!";
    private readonly string nullDigitString = "_";
    private readonly string historyTitleString = "Your Tries:\n";
    private readonly string[] historyStrings = 
	{" 1st:", " 2nd:",  " 3rd:", " 4th:", " 5th:", " 6th:", "final:"};
    private readonly string betTitleString = "Bet:\n";
    private readonly string rewardAmountTitleString = "Reward Amount:\n$ ";
    private readonly string rewardTitleString = "Reward:\n$ ";
    private readonly string creditBalanceTitleString = "Credit Balance:\n$ ";


    private GameObject[] digitTexts = new GameObject[ 4 ];
    private GameObject[] digitPanels = new GameObject[ 4 ];
    private GameObject historyText;
    private GameObject betText;
    private GameObject rewardText;
    private GameObject rewardAmountText;
    private GameObject creditBalanceText;
    private GameObject devicePanel;
    private GameObject messagePanel;
    private GameObject messageText;
    private GameObject fullAutoButtonText;
    private GameObject debugPanelText;

    private int hitCount;
    private bool fullAuto = false;

    private int[] wins = null;
    private int failCount,tryCount;
    private int spend=0,earn=0;
    
    public Game(){
    }

    void Start(){
	digitTexts[0] = GameObject.Find( "Digit0Text" );
	digitTexts[1] = GameObject.Find( "Digit1Text" );
	digitTexts[2] = GameObject.Find( "Digit2Text" );
	digitTexts[3] = GameObject.Find( "Digit3Text" );
	digitPanels[0] = GameObject.Find( "Digit0Panel" );
	digitPanels[1] = GameObject.Find( "Digit1Panel" );
	digitPanels[2] = GameObject.Find( "Digit2Panel" );
	digitPanels[3] = GameObject.Find( "Digit3Panel" );

	historyText = GameObject.Find( "HistoryText" );

	betText = GameObject.Find( "BetText" );
	creditBalanceText = GameObject.Find( "CreditText" );

	messagePanel = GameObject.Find( "MessagePanel" );
	messageText = GameObject.Find( "MessageText" );

	devicePanel = GameObject.Find( "DevicePanel" );
	rewardText = GameObject.Find( "RewardText" );
	rewardAmountText = GameObject.Find( "RewardAmountText" );

	fullAutoButtonText = GameObject.Find( "FullAutoButtonText" );

	debugPanelText = GameObject.Find( "DebugPanelText" );

	status.SetHttpUtility( GameObject.Find("Http").GetComponent<HttpUtility>() );

	wins = new int[ status.maxTries ];
	for( int i=0; i<status.maxTries; ++i ){
	    wins[i] = 0;
	}
    }

    void Update(){
    }

    //////////////////////////////////////////////
    //Utility,
    //////////////////////////////////////////////
    void UpdateDraw(){
	status.CalcReward();//これはしょっちゅう変わるので。
	drawReq = true;
    }

    //////////////////////////////////////////////
    //Updates
    //////////////////////////////////////////////
    public void Init(){
	status.ChangePhase( GameStatus.PHASE.LOGIN );
    }
    public void Login(){
	switch( status.phaseStatus ){
	case 0://リクエスト発行。
	    if( status.LoginTransaction() ) ++status.phaseStatus;
	    break;
	case 1://レスポンス待ち。
	    if( status.IsTransactionSuccess() ){
		status.LoginTransactionDone();
		status.ChangePhase( GameStatus.PHASE.NEW_GAME );
	    }else if( status.IsTransactionFailure() ){
	    }
	    break;
	default:
	    break;
	}
    }
    public void NewGame(){
	hitCount = 0;
	switch( status.phaseStatus ){
	case 0://リクエスト発行。
	    messagePanel.SetActive(false);
	    devicePanel.SetActive(true);
	    status.NewGameTransaction();
	    ++status.phaseStatus;
	    break;
	case 1://レスポンス待ち。
	    if( status.IsTransactionSuccess() ){
		status.AdmitTransaction();
		status.ChangePhase( GameStatus.PHASE.SELECT );
	    }else if( status.IsTransactionFailure() ){
	    }
	    break;
	default:
	    break;
	}
    }
    public void Select(){
	messagePanel.SetActive(false);
	devicePanel.SetActive(true);

	if( fullAuto ){
	    if( status.phaseStatus == 2 ){
		//GiveUpButtonでNEW_GAME。
		status.ChangePhase( GameStatus.PHASE.GIVE_UP );
		IncFail();
	    }else if( (status.phaseCounter % 30) == 0 ){
		if( status.IsDigitsFull() ){
		    status.ChangePhase( GameStatus.PHASE.TRY );
		}else{
		    status.ExecAutoAtCurrentCursor();
		}
	    }
	}else{
	    if( status.phaseStatus == 1 ){
		//ExecButtonでTRY。
		status.ChangePhase( GameStatus.PHASE.TRY );
	    }else if( status.phaseStatus == 2 ){
		//GiveUpButtonでNEW_GAME。
		status.ChangePhase( GameStatus.PHASE.GIVE_UP );
		IncFail();
	    }
	}
    }
    public void Try(){
	messagePanel.SetActive(false);
	devicePanel.SetActive(true);
	switch( status.phaseStatus ){
	case 0://リクエスト発行。
	    status.TryTransaction();
	    ++status.phaseStatus;
	    spend += status.Bet();
	    break;
	case 1://レスポンス待ち。
	    if( status.IsTransactionSuccess() ){
		status.TryTransactionDone();
		if( status.IsWin() ){
		    status.ChangePhase( GameStatus.PHASE.WIN );
		}else if( status.IsFinalTry() ){
		    status.ChangePhase( GameStatus.PHASE.FINALE );
		}else{
		    status.ChangePhase( GameStatus.PHASE.RESULT );
		}
	    }else if( status.IsTransactionFailure() ){
	    }
	    break;
	default:
	    break;
	}
    }
    public void GiveUp(){
	status.ChangePhase( GameStatus.PHASE.NEW_GAME );
    }
    public void Result(){
	if( status.phaseStatus == 0 ){
	    ++status.phaseStatus;
	    int old = hitCount;
	    hitCount = status.GetCrackedDigitsCount();
	    messageText.GetComponent<Text>().text = " " + (hitCount - old) + " Hits";
	}else {
	    if( status.phaseCounter > 15 ){
		messagePanel.SetActive(true);
		devicePanel.SetActive(true);
	    }
	    if( status.phaseCounter > 45 ){
		status.StartNextTry();
		status.ChangePhase( GameStatus.PHASE.SELECT );
	    }
	}
    }
    private void IncFail(){
	++failCount;
	++tryCount;
    }
    private void IncWin(){
	wins[ status.TryCount() ] += 1;
	++tryCount;
    }
    public void Finale(){
	if( status.phaseCounter > 60 ){
	    messagePanel.SetActive(true);
	    devicePanel.SetActive(true);
	    messageText.GetComponent<Text>().text = YouFailedString;
	}
	if( status.phaseCounter > 120 ){
	    status.ChangePhase( GameStatus.PHASE.NEW_GAME );
	    IncFail();
	}
    }
    public void Win(){
	if( status.phaseCounter > 60 ){
	    messagePanel.SetActive(true);
	    devicePanel.SetActive(true);
	    messageText.GetComponent<Text>().text = YouWinString;
	}
	if( status.phaseCounter > 120 ){
	    status.ChangePhase( GameStatus.PHASE.NEW_GAME );
	    IncWin();
	    earn += status.Reward();
	}
    }
    public void Error(){
	if( status.phaseStatus == 1 ){
	    status.ChangePhase( GameStatus.PHASE.LOGOUT );
	}
    }
    public void Logout(){
    }

    //////////////////////////////////////////////
    //Proc
    //////////////////////////////////////////////
    public void Proc(){
	switch( status.Phase() ){
	case GameStatus.PHASE.INIT:
	    Init();
	    break;
	case GameStatus.PHASE.LOGIN:
	    Login();
	    break;
	case GameStatus.PHASE.NEW_GAME:
	    NewGame();
	    break;
	case GameStatus.PHASE.SELECT:
	    Select();
	    break;
	case GameStatus.PHASE.TRY:
	    Try();
	    break;
	case GameStatus.PHASE.GIVE_UP:
	    GiveUp();
	    break;
	case GameStatus.PHASE.RESULT:
	    Result();
	    break;
	case GameStatus.PHASE.FINALE:
	    Finale();
	    break;
	case GameStatus.PHASE.WIN:
	    Win();
	    break;
	case GameStatus.PHASE.LOGOUT:
	    Logout();
	    break;
	}
	//@todo 
	UpdateDraw();
    }
    //////////////////////////////////////////////
    //Draw
    //////////////////////////////////////////////
    public void SetDigitPanel( int i ){
	Color c = new Color();
	//( digitPanels[i].GetComponent<Image>().color );
	if( status.IsCrackedDigitAt( i ) ){
	    c.r = 0.5f;
	    c.g = 1.0f;
	    c.b = 0.5f;
	    c.a = 1.0f;
	}else{
	    c.a = ( status.Cursor() >= i ) ? 1.0f : 0.5f;
	    c.r = 0.9f;
	    c.g = 0.9f;
	    c.b = 0.9f;
	}
	digitPanels[i].GetComponent<Image>().color = c;
    }
    public void Draw(){

	if( !drawReq ) return;

	//4けたの数字
	for( int i=0; i<status.maxDigits; ++i ){
	    //未設定。
	    digitTexts[ i ].GetComponent<Text>().text = 
		( status.GetDigitAt( i ) < 0 ) ?  
		nullDigitString : status.GetDigitAt(i).ToString();
	    SetDigitPanel( i );
	}

	//履歴
	string str = historyTitleString;
	int showLine = status.TryCount();//通常
	if( status.IsPhase(GameStatus.PHASE.FINALE) ) showLine = status.maxTries;
	if( status.IsPhase(GameStatus.PHASE.WIN) ) showLine += 1;
	for( int i=0; i<status.maxTries; ++i ){
	    //未設定。
	    str += historyStrings[i];

	    if( i < showLine ){
		//すでに決まっている。
		for( int j=0; j<status.maxDigits; ++j ){
		    str += (" " + status.GetDigitAt(i,j) );
		}
	    }else{
		for( int j=0; j<status.maxDigits; ++j ){
		    str += (" " + nullDigitString);
		}
	    }
	    str += "\n";
	}
	historyText.GetComponent<Text>().text = str;


	//ベット
	betText.GetComponent<Text>().text = betTitleString + status.Bet();

	//クレジット
	creditBalanceText.GetComponent<Text>().text = creditBalanceTitleString + status.CreditBalance();

	//賞金
	//rewardAmountText.GetComponent<Text>().text = rewardAmountTitleString + status.Reward();
	rewardText.GetComponent<Text>().text = rewardTitleString + status.Reward();



	//debug
	str = "Statistics:\n\n";
	str += ("1st wins:  " + wins[0] + "\n");
	str += ("2nd wins:  " + wins[1] + "\n");
	str += ("3rd wins:  " + wins[2] + "\n");
	str += ("4th wins:  " + wins[3] + "\n");
	str += ("5th wins:  " + wins[4] + "\n");
	str += ("6th wins:  " + wins[5] + "\n");
	str += ("fin wins:  " + wins[6] + "\n");
	str += ("fails   :  " + failCount + "\n");
	str += ("rounds  :  " + tryCount + "\n\n");
	str += ("earned  :  " + earn + "\n" );
	str += ("spent   :  " + spend + "\n" );
	debugPanelText.GetComponent<Text>().text = str;

	drawReq = false;
    }

    //////////////////////////////////////////////
    //On callback
    //////////////////////////////////////////////
    public void OnUpdate(){
	Proc();
	Draw();
    }

    public void OnBetUpButton(){
	if( status.UpBet() ){
	    UpdateDraw();
	}
    }

    public void OnBetDownButton(){
	if( status.DownBet() ){
	    UpdateDraw();
	}
    }

    public void OnAutoButon(){
	status.ExecAuto();
    }

    public void OnExecButon(){
	if( status.IsDigitsFull() ) status.phaseStatus = 1;
    }

    public void OnDigitButton( int no ){
	if( status.SetCurrentDigit( no ) ){
	    UpdateDraw();
	}
    }

    public void OnClearButton(){
	if( status.ClearCurrentDigit() ){
	    UpdateDraw();
	}
    }

    public void OnGiveUp(){
	status.phaseStatus = 2;
    }

    public void OnFullAuto(){
	fullAuto = !fullAuto;
	if( fullAuto ){
	    fullAutoButtonText.GetComponent<Text>().text = "Play Mode:\nFull Auto";
	}else{
	    fullAutoButtonText.GetComponent<Text>().text = "Play Mode:\nManual";
	}
    }


}