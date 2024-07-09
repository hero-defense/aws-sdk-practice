using System;
using System.Collections.Generic;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using UnityEngine;
using UnityEngine.Events;

// https://docs.aws.amazon.com/ko_kr/gamelift/latest/developerguide/integration-engines-unity-using.html
// https://docs.aws.amazon.com/ko_kr/gamelift/latest/developerguide/gamelift-sdk-server-api.html
public class GameLiftConnect : MonoBehaviour {
  [field: SerializeField] public int ListeningPort { get; } = 7777;

  [field: SerializeField]
  public string WebsocketUrl { get; }
    = "wss://us-west-2.api.amazongamelift.com";

  [field: SerializeField] public string ProcessId { get; } = "myProcess";
  [field: SerializeField] public string HostId { get; } = "myHost";
  [field: SerializeField] public string FleetId { get; } = "myFleet";
  [field: SerializeField] public string AuthToken { get; } = "myAuthToken";


  public UnityEvent<GameSession> onStartGameSesion;
  public UnityEvent<UpdateGameSession> onUpdateGameSession;
  public UnityEvent onProcessTerminate;
  public UnityEvent<bool> onHealthCheck;

  void Start() {
    // required for a GameLift Anywhere fleet.
    // not required for a GameLift managed EC2 fleet.
    var serverParams = new ServerParameters(
      WebsocketUrl,
      ProcessId,
      HostId,
      FleetId,
      AuthToken);

    // establishes a local connection with an Amazon GameLift agent
    // in order to enable further communication.
    var initSdkOutcome = GameLiftServerAPI.InitSDK(serverParams);
    if (initSdkOutcome.Success) {
      var processParams = new ProcessParameters(
        onStartGameSession: gameSession => {
          // send a game session activation request to the game server.
          // with game session object containing game properties and other settings.
          // here is where a game server takes action based on the game session object.
          // when the game server is ready to receive incoming player connections,
          // it invokes the server SDK call ActivateGameSession()
          GameLiftServerAPI.ActivateGameSession();
          onStartGameSesion?.Invoke(gameSession);
        },
        onUpdateGameSession: updateGameSession => {
          // GameLift calls back a request when a game session is updated
          // such as for FlexMatch backfill, with an updated game sesion object.
          // the game server can examine matchmakerData and handle
          // new incoming players.
          // updateReason explains the purpose of the update.
          switch (updateGameSession.UpdateReason) {
            case UpdateReason.MATCHMAKING_DATA_UPDATED:
              break;
            case UpdateReason.BACKFILL_FAILED:
              break;
            case UpdateReason.BACKFILL_TIMED_OUT:
              break;
            case UpdateReason.BACKFILL_CANCELLED:
              break;
            case UpdateReason.UNKNOWN:
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }

          onUpdateGameSession?.Invoke(updateGameSession);
        },
        onProcessTerminate: () => {
          // GameLift calls back before shutting down the instance hosting this game server.
          // it gives the game server a change to save its state,
          // communicate with services, etc.
          // and initiate shut down. when the game server is ready to shut down,
          // it invokes the server SDK call ProcessEnding() to tell GameLift 
          // it is shutting down.
          GameLiftServerAPI.ProcessEnding();
          onProcessTerminate?.Invoke();
        },
        onHealthCheck: () => {
          // GameLift calls this back about every 60 seconds.
          // a game server might want to check the health of dependencies, etc.
          // then it returns health status true if healthy, false otherwise.
          // the game server must respond within 60 seconds, or GameLift records 'false'.
          // In this example, the game server always reports healthy.
          bool bResult = true;
          onHealthCheck?.Invoke(bResult);

          return bResult;
        },
        // the game server gets ready to report that it is to host game sessions
        // and that it will listen on port 7777 for incoming player connections.
        port: ListeningPort,
        logParameters: new LogParameters(new List<string> {
          // the game server tells GameLift where to find game session log files.
          // at the end of a game session, GameLift uploads everything in the specified
          // location and stores it in the cloud for access later.
          "/local/game/logs/myserver.log"
        }));

      // the game server calls ProcessReady() to tell GameLift it's ready to host game sessions.
      var processReadyOutcome = GameLiftServerAPI.ProcessReady(processParams);
      if (processReadyOutcome.Success) {
        print("ProcessReady success.");
      }
      else {
        print("ProcessReady failure: " + processReadyOutcome.Error);
      }
    }
    else {
      print("InitSDK failure: " + initSdkOutcome.Error);
    }
  }

  void OnApplicationQuit() {
    // Make sure to call GameLiftServerAPI.ProcessEnding() and
    // GameLiftServerAPI.Destroy() before terminating the server process.
    // These actions notify Amazon GameLift that the process is terminating and 
    // frees the API client from memory.
    var processEndingOutcome = GameLiftServerAPI.ProcessEnding();
    GameLiftServerAPI.Destroy();
    if (processEndingOutcome.Success) {
      Environment.Exit(0);
    }
    else {
      Console.WriteLine("ProcessEnding() failed. Error: " +
                        processEndingOutcome.Error);
      Environment.Exit(-1);
    }
  }
}