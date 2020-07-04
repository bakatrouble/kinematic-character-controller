using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToggleScene : MonoBehaviour {
    private void Awake() {
        // SceneManager.LoadScene(0);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            StartCoroutine(LoadLevel("Slopes"));
        } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            StartCoroutine(LoadLevel("Planet"));
        } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            StartCoroutine(LoadLevel("Planet Hollow"));
        } else if (Input.GetKeyDown(KeyCode.L)) {
            for (var i = 0; i < 10000; i++) {
                Debug.Log(i);
            }
        }
    }

    private IEnumerator LoadLevel(string n) {
        var scene = SceneManager.GetSceneByName(n);
        if (!scene.isLoaded) {
            var asyncLoad = SceneManager.LoadSceneAsync(n);
            while (!asyncLoad.isDone) {
                yield return null;
            }
        }

        SceneManager.SetActiveScene(scene);
    }
}