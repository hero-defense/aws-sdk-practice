using System.Collections;
using System.Collections.Generic;
using Aws.GameLift.Server.Model;
using UnityEngine;

public class Entry : MonoBehaviour {
  public void OnStartSession(GameSession gameSession) {
    print($"[{nameof(OnStartSession)}] session started\n{gameSession}");
  }

  public void OnUpdateGameSession(UpdateGameSession updateGameSession) {
    print(
      $"[{nameof(OnUpdateGameSession)}] session updated\n{updateGameSession}");
  }

  public void OnProcessTerminate() {
    print($"[{nameof(OnProcessTerminate)}] process terminated");
  }

  public void OnHealthCheck(bool isHealthy) {
    print($"[{nameof(OnHealthCheck)}] health check {isHealthy}");
  }
}