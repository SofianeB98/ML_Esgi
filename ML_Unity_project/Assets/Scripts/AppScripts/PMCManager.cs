﻿using System;
using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;
using Random = UnityEngine.Random;

public class PMCManager : MonoBehaviour
{
    private static PMCManager instance;
    public static PMCManager Instance => PMCManager.instance;

    //Peut etre placer dans la fonction de train
    public TextureClass[] datasets = new TextureClass[0];
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(this);
    }

    #region Machine Learning Functions

    public void CreateModel()
    {
        if (!this.enabled || !this.gameObject.activeInHierarchy)
            return;

        //On vérifie que les input size dans npl[0]
        //soit cohérent avec la tailles des textures
        int isize = TexturesDataset.completeDatasetByClasses[0][0].width *
                    TexturesDataset.completeDatasetByClasses[0][0].height;
        if (MLParameters.NPL.Length > 0)
        {
            if (!MLParameters.NPL[0].Equals(isize))
                MLParameters.NPL[0] = isize;

            Debug.Log($"NPL 0 = {MLParameters.NPL[0]}");
            
            MLParameters.Input_size = MLParameters.NPL[0];
            MLParameters.Output_size = MLParameters.NPL[MLParameters.NPL.Length - 1];
        }
        else
        {
            MLParameters.NPL = new[] {isize, 1};
            MLParameters.Input_size = MLParameters.NPL[0];
            MLParameters.Output_size = MLParameters.NPL[MLParameters.NPL.Length - 1];
        }

        //On crée notre model
        MLParameters.model = MLDLLWrapper.CreateModel(MLParameters.NPL, MLParameters.NPL.Length);
        Debug.Log("Modèle créé \n");
    }

    public void TrainModel()
    {
        if (!enabled || !this.gameObject.activeInHierarchy)
            return;

        if (MLParameters.model.Equals(IntPtr.Zero))
        {
            Debug.LogError("You trying to train your model, but it's not created");
            return;
        }

        //On crée notre dataset selon le poucentage que l'on veut utiliser
        //On va prendre autant de texture de chaque classe
        //On cherche d'abord la classe qui va nous donner le moins de texture avec le poucentage voulu
        int texCounts = -1;
        for (int i = 0; i < TextureLoader.Instance.foldersName.Length; i++)
        {
            if (texCounts == -1)
                texCounts = Mathf.RoundToInt(TexturesDataset.completeDatasetByClasses[i].Length *
                                             MLParameters.UseDatasetAsNPercent);
            else
                texCounts = texCounts > Mathf.RoundToInt(TexturesDataset.completeDatasetByClasses[i].Length *
                                                         MLParameters.UseDatasetAsNPercent)
                    ? Mathf.RoundToInt(TexturesDataset.completeDatasetByClasses[i].Length *
                                       MLParameters.UseDatasetAsNPercent)
                    : texCounts;
        }

        double[] inputs_dataset = new double[0];
        double[] outputs = new double[0];

        for (int tr = 0; tr < MLParameters.TrainLoopCount; tr++)
        {
            //On crée le tableau de texture avec autant de counts par classe
            datasets = new TextureClass[texCounts * TextureLoader.Instance.foldersName.Length];
            int idx = 0;
            //on remplit le tableau
            for (int i = 0; i < TextureLoader.Instance.foldersName.Length; i++)
            {
                List<int> randomIndex = new List<int>();

                if (!TexturesDataset.unusedDatasetByClasses.ContainsKey(i))
                    TexturesDataset.unusedDatasetByClasses.Add(i,
                        new Texture2D[TexturesDataset.completeDatasetByClasses[i].Length - texCounts]);

                for (int j = 0; j < texCounts; j++)
                {
                    //on tire un index au hasard
                    int rdm = Random.Range(0, TexturesDataset.completeDatasetByClasses[i].Length);
                    int ite = 0;
                    while ((randomIndex.Contains(rdm) && randomIndex.Count >= 1) ||
                           ite >= TexturesDataset.completeDatasetByClasses[i].Length)
                    {
                        rdm = Random.Range(0, TexturesDataset.completeDatasetByClasses[i].Length);
                        ite++;
                    }

                    randomIndex.Add(rdm);

                    //on ajoute la texture
                    datasets[idx] = new TextureClass();
                    datasets[idx].tex = TexturesDataset.completeDatasetByClasses[i][rdm];
                    datasets[idx].classe = i;
                    idx++;
                }

                // int tmp = 0;
                // for (int j = 0; j < TexturesDataset.completeDatasetByClasses[i].Length; j++)
                // {
                //     if (randomIndex.Contains(j))
                //         continue;
                //
                //     TexturesDataset.unusedDatasetByClasses[i][tmp] = TexturesDataset.completeDatasetByClasses[i][j];
                //     tmp++;
                // }
            }

            //On remplit de double[] array
            inputs_dataset = new double[datasets.Length * MLParameters.Input_size];
            outputs = new double[datasets.Length * MLParameters.Output_size];
            idx = 0;
            int idx_out = 0;
            for (int n = 0; n < datasets.Length; n++)
            {
                for (int i = 0; i < datasets[n].tex.width; i++)
                {
                    for (int j = 0; j < datasets[n].tex.height; j++)
                    {
                        inputs_dataset[idx] = datasets[n].tex.GetPixel(i, j).grayscale;
                        idx++;
                    }
                }

                if (MLParameters.Output_size == 1)
                {
                    outputs[idx_out] = datasets[n].classe == 0 ? -1 : 1;
                    idx_out++;
                }
                else
                {
                    //double[] tmpOut = new double[TextureLoader.Instance.foldersName.Length];
                    for (int i = 0; i < MLParameters.Output_size; i++)
                    {
                        if (i == datasets[n].classe)
                            outputs[idx_out] = 1.0;
                        else
                            outputs[idx_out] = 0.0;

                        idx_out++;
                    }
                }
            }

            MLParameters.SampleCounts = datasets.Length;

            //Enfin, on entraine notre modèle N fois
            Debug.Log("On entraîne le modèle\n...");
            MLDLLWrapper.Train(MLParameters.model, inputs_dataset, outputs,
                MLParameters.SampleCounts, MLParameters.Epochs, MLParameters.Alpha, MLParameters.IsClassification);
            Debug.Log("Modèle entrainé \n");
        }
    }

    public void Predict(Texture2D card, out double accuracy, out string pred)
    {
        accuracy = -1.0;
        pred = "-";
        if (!enabled || !this.gameObject.activeInHierarchy)
            return;

        if (MLParameters.model.Equals(IntPtr.Zero))
        {
            Debug.LogError("You trying to predict these inputs, but your model is not created");
            return;
        }

        Debug.Log("Prediction de la carte selectionnée !\n");
        double[] inputTmp = new double[MLParameters.Input_size];


        for (int i = 0; i < card.width; i++)
        {
            for (int j = 0; j < card.height; j++)
            {
                inputTmp[i * card.width + j] = card.GetPixel(i, j).grayscale;
            }
        }

        var res = MLDLLWrapper.Predict(MLParameters.model, inputTmp, MLParameters.IsClassification);
        double[] resFromPtr = new double[MLParameters.Output_size + 1];
        System.Runtime.InteropServices.Marshal.Copy(res, resFromPtr, 0, MLParameters.Output_size + 1);

        int foldId = MLParameters.GetIndexOfHigherValueInArray(resFromPtr);

        for (int i = 1; i < resFromPtr.Length; i++)
        {
            Debug.LogWarning($"RESULTAT ==> valeur {i} = {resFromPtr[i].ToString("0.0000")}");
        }

        pred = TextureLoader.Instance.foldersName[foldId];
        accuracy = MLParameters.Output_size == 1 ? (float) resFromPtr[1] : (float) resFromPtr[foldId + 1];
        
        Debug.LogWarning($"Prediction : {pred} \n " +
                         $"Pourcentage : {accuracy.ToString("0.000")}");
        
        
        
        MLDLLWrapper.DeleteDoubleArrayPtr(res);
    }

    public void DeleteModel()
    {
        if (MLParameters.model.Equals(IntPtr.Zero))
            return;
        
        MLDLLWrapper.DeleteModel(MLParameters.model);
        MLParameters.model = IntPtr.Zero;
        Debug.Log("Modèle détruit\n");
    }

    public void SaveModel()
    {
        MLP mlp = new MLP();
        mlp.W = new List<ListOfListDouble>();
        mlp.NPL = MLParameters.NPL;
        
        var layer_counts = MLParameters.NPL.Length;
        
        for (int l = 0; l < layer_counts; ++l)
        {
            ListOfListDouble a = new ListOfListDouble();
            a.Wi = new List<ListOfDouble>();
            
            if (l == 0)
            {
                ListOfDouble b = new ListOfDouble();
                b.Wj = new List<double>();
                b.Wj.Add(1.0);
                a.Wi.Add(b);
            }
            else
            {
                for (int i = 0; i < MLParameters.NPL[l - 1] + 1; ++i)
                {
                    ListOfDouble b = new ListOfDouble();
                    b.Wj = new List<double>();

                    for (int j = 0; j < MLParameters.NPL[l] + 1; ++j)
                    {
                        b.Wj.Add(Random.value);
                    }

                    a.Wi.Add(b);
                }
            }

            mlp.W.Add(a);
        }
        
        for (int l = 0; l < mlp.W.Count; l++)
            for (int i = 0; i < mlp.W[l].Wi.Count; i++)
                for (int j = 0; j < mlp.W[l].Wi[i].Wj.Count; j++)
                    mlp.W[l].Wi[i].Wj[j] = MLDLLWrapper.GetWeightValueAt(MLParameters.model, l, i, j);
        
        //save
        var str = JsonUtility.ToJson(mlp, true);
        var path = Path.Combine(Application.dataPath, "SavedModels");
        path = Path.Combine(path, "pmcCard.json");

        path = StandaloneFileBrowser.SaveFilePanel("title", Application.dataPath, "namefile", "json");
        Debug.Log($"{path}");
        
       File.WriteAllText(path,str);

       mlp.W.Clear();
        
        Debug.Log("Modele sauvegarde !!");
    }

    public void LoadModel(string path)
    {
        var str = File.ReadAllText(path);
        var loadedModel = JsonUtility.FromJson<MLP>(str);
        
        if (MLParameters.model.Equals(System.IntPtr.Zero))
        {
            MLParameters.NPL = loadedModel.NPL;
            MLParameters.IsClassification = true;
            CreateModel();
        }
        
        for (int l = 0; l < loadedModel.W.Count; l++)
            for (int i = 0; i < loadedModel.W[l].Wi.Count; i++)
                for (int j = 0; j < loadedModel.W[l].Wi[i].Wj.Count; j++)
                   MLDLLWrapper.SetWeightValueAt(MLParameters.model, l, i, j, loadedModel.W[l].Wi[i].Wj[j]);
    }
    #endregion
}