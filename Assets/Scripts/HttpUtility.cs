using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using LitJson;

// 	public class RequestParameters : MonoBehaviour
// 	{
// }

public class HttpUtility : MonoBehaviour {
    ////////////////////////////////////////////////////////////////////
    //Singleton
    ////////////////////////////////////////////////////////////////////
//     private static HttpUtility instance = null;
//     public static HttpUtility Instance {
//         get {
//             if( instance == null ) instance = new HttpUtility();
//             return instance;
//         }
//     }

    bool isDone;
    string msg;
    string responseText;

    void Start(){
	isDone = false;
    }


    public string testParameters = "?author=Bernard%20Fran%C3%A7ois&company=PreviewLabs";
	
    private static Dictionary<string, string> parameters = new Dictionary<string, string>(); 
    
    public static bool HasKey(string key)
    {
	return parameters.ContainsKey (key);
    }
			
    // This can be called from Start(), but not earlier
    public static string GetValue(string key)
    {
	return parameters[key];
    }

    public void Awake ()
    {
	Application.ExternalEval(		
				 " UnityObject2.instances[0].getUnity().SendMessage('" + name + "', 'SetRequestParameters', document.location.search);"
						);	
	
	#if UNITY_EDITOR
	    SetRequestParameters(testParameters);
	#endif
    }
    
		
    public void SetRequestParameters(string parametersString)
    {
	char[] parameterDelimiters = new char[]{'?', '&'};
	string[] parameters = parametersString.Split (parameterDelimiters, System.StringSplitOptions.RemoveEmptyEntries);
	
	
	char[] keyValueDelimiters = new char[]{'='};
	for (int i=0; i<parameters.Length; ++i)
	    {
		string[] keyValue = parameters[i].Split (keyValueDelimiters, System.StringSplitOptions.None);
		
		if (keyValue.Length >= 2)
		    {
			HttpUtility.parameters.Add(WWW.UnEscapeURL(keyValue[0]), WWW.UnEscapeURL(keyValue[1]));
		    }
		else if (keyValue.Length == 1)
		    {
			HttpUtility.parameters.Add(WWW.UnEscapeURL(keyValue[0]), "");
		    }	
	    }			
    }

    // レスポンスのJSONを格納するクラス
    class TestResponse {
	public string name;
	public int level;
	public List<string> friend_names;
    }

    //
    //WWWForm form = null;
    public bool SendRequest( string url, string xml ){
	StartCoroutine (Post( url, xml ));
	return true;
    }
	
// 	IEnumerator Download()
// 	{

// 		WWW www = new WWW("http://hoge/two.json?num=3");
// 		//  wait until JSON Contents will come 
// 		yield return www;

// 		if(www.error != null){
// 			Debug.Log("Error!");

// 		}else{
// 			Debug.Log("Success");

// 			//parse the JSON
// 			var jsonData = MiniJSON.Json.Deserialize(www.text) as Dictionary<string,object>;
// 			//string name = (string)jsonData["multiply"];
// 			//long num = (long)jsonData["num"];
// 			long num = (long)jsonData["multiply"];
// 			int tmp = (int)num;

// 			for(int i = 0; i < tmp; i++){
// 				Instantiate(this.sphere,this.transform.position,Quaternion.identity);
// 			}

// 		}
// 	}

    public bool IsDone(){
	return isDone;
    }
    public bool IsSuccess(){
	return IsDone() && msg == null;
    }
    public bool IsFailure(){
	return IsDone() && msg != null;
    }
    public string GetResponseText(){
	return responseText;
    }

    IEnumerator Post (string url, string xml) {
	isDone = false;
	msg = null;
	responseText = null;
	
	//これを足すと勝手にPostになるらしい。
	WWWForm form = new WWWForm();
	form.AddField ("commonData", xml);

        WWW www = new WWW (url, form);

        // 送信開始
	Debug.Log("Sending Request! " + url);
	Debug.Log(xml);
        yield return www;

	Debug.Log("Sent!");

        // 成功
	isDone = true;
	msg = www.error;
	responseText = System.Text.Encoding.UTF8.GetString(www.bytes, 0, www.bytes.Length - 0);
	//responseText = www.text;

        if (msg == null) {
	    //Debug.Log( responseText );
            Debug.Log("Get Success");
        } // 失敗
        else{
            Debug.Log("Get Failure: "+ www.error );
        }
    }
//     IEnumerator Post (string url, string xml) {
//         // HEADERはHashtableで記述
// 	Dictionary<string,string> header = new Dictionary<string,string>();
//         header.Add ("Content-Type", "application/xml; charset=UTF-8");
//         byte[] postBytes = Encoding.Default.GetBytes (xml);

//         // 送信開始
//         WWW www = new WWW (url, postBytes, header);
//         yield return www;

//         // 成功
//         if (www.error == null) {
//             Debug.Log("Post Success");
//         }
//         // 失敗
//         else{
//             Debug.Log("Post Failure");          
//         }
//     }

//     IEnumerator Post()
//     {
// 	JsonData data = new JsonData();

//         data["name"] = "o-kuhiiro";
//         data["age"] = 30;
//         // bodyを作成
//         byte[] postBytes = Encoding.Default.GetBytes (data.ToJson());

//         string url = "http://google.com/";
//         //string url = "サイズが大きいファイルなどをいれるとタイムアウト";

//         HTTP.Request r = new HTTP.Request ("POST", url, postBytes);

//         // Headerを作成
//         r.headers.Add ("Content-Type", "application/json; charset=UTF-8");
//         r.headers.Add ("User-Agent", "iphone6Plus");

// 	// タイムアウト秒数を設定
//         r.timeout = 3;

//         yield return r.Send();

// 	if (r.exception != null) {
// 	    // なにかエラー発生
// 	    Debug.Log ("post request error: " + r.exception.ToString ());
	    
// 	    // タイムアウト発生
// 	    if (r.exception is System.TimeoutException) {
// 		Debug.Log ("Request timed out.");
// 		// それ以外のエラー
// 	    } else {
// 		Debug.Log ("Exception occured in request.");
// 	    }
// 	} else if (r.response.status != 200) { 
// 	    Debug.Log ("post request code:" + r.response.status);
// 	    // 成功
// 	} else {
// 	    Debug.Log ("post Success!!");
// 	    Debug.Log ("returned data:" + r.response.Text);
// 	}


// // 	WWWForm form = new WWWForm();
// // 	form.AddField ("num", "4");
// // 	WWW www = new WWW ("http://hoge/two.json", form);

// // 	//  wait until JSON Contents will come 
// // 	// タイムアウト秒数を設定
// //         www.timeout = 3;
// // 	yield return www;

// // 	    //parse the JSON
// // 	    var jsonData = MiniJSON.Json.Deserialize(www.text) as Dictionary<string,object>;
// // 	    //string name = (string)jsonData["multiply"];
// // 	    //long num = (long)jsonData["num"];
// // 	    long num = (long)jsonData["multiply"];
// // 	    int tmp = (int)num;

// // 	    Debug.Log("Success " + tmp);
	    
	    
// // 	    for(int i = 0; i < tmp; i++){
// // 		Instantiate(this.sphere,this.transform.position,Quaternion.identity);
// // 	    }
// //	}
//     }
}


