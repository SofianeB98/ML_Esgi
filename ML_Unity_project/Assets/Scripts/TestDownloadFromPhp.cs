﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;



public class TestDownloadFromPhp : MonoBehaviour
{
    
    private void Start()
    {
        //StartCoroutine(TestPhp());
    }

    public CarteData data;
    
    // private IEnumerator TestPhp()
    // {
    //     var path = Path.Combine(Application.dataPath, "testygo.json");
    //     string realUrl = "https://db.ygoprodeck.com/api/v7/cardinfo.php";
    //     using (UnityWebRequest request = UnityWebRequest.Get(realUrl))
    //     {
    //         //Debug.LogWarning("Lancement de la request");
    //         // Send the request and wait for a response
    //         yield return request.SendWebRequest();
    //         var txt = request.downloadHandler.text;
    //         //string[] separatingStrings = {"\"id\":"};
    //         //var texts = txt.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);
    //         
    //         data = JsonUtility.FromJson<CarteData>(txt);
    //         txt = JsonUtility.ToJson(data, true);
    //         File.WriteAllText(path, txt);
    //     }
    //
    //
    //     Debug.LogWarning("DOWNLOAD FINI !");
    //
    //     yield break;
    // }
}