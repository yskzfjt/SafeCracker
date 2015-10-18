using UnityEngine;
using System.Collections;

public class SystemScript : MonoBehaviour {
    Game game = null;
    GameStatus status = GameStatus.Instance;

    // Use this for initialization
    void Start () {
 	game = GameObject.Find( "Game" ).GetComponent<Game>();
    }

    // Update is called once per frame
    void Update () {
	game.OnUpdate();
	status.Update();
// 	if( status.IsTransactionWait() ){
// 	    //ゲームのステータスに応じてWebAPIを切り替え。
// 	    switch( status.Phase() ){
// 	    case GameStatus.PHASE.INIT:
// 		break;
// 	    case GameStatus.PHASE.LOGIN:
// 		break;
// 	    case GameStatus.PHASE.NEW_GAME:
// 		break;
// 	    case GameStatus.PHASE.SELECT:
// 		break;
// 	    case GameStatus.PHASE.TRY:
// 		//dummy
// 		for( int i=0; i<status.maxDigits; ++i ){
// 		    if( status.GetCrackedDigitAt(i) < 0 &&
// 			status.IsUniqueDigitAt(i) && 
// 			Random.Range( 0, 1000 ) < 200 ){
// 			status.CrackDigitAt(i);
// 		    }
// 		}
// 		break;
// 	    case GameStatus.PHASE.GIVE_UP:
// 		break;
// 	    case GameStatus.PHASE.RESULT:
// 		break;
// 	    case GameStatus.PHASE.FINALE:
// 		break;
// 	    case GameStatus.PHASE.LOGOUT:
// 		break;
// 	    }
// 	    status.SetTransactionDone();
// 	}
    }

    
    

}
