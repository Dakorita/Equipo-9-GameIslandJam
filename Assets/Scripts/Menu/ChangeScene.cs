using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [SerializeField]
    private string scene; 
    public void ChangeScenes()
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
    
}