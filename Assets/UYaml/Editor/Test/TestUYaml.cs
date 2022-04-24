/**************************************************************
 *  Filename:    TestUYaml.cs
 *  @author:     wuliangyu
 *  @version     2022-02-24
 **************************************************************/

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

using UYaml;

public class TestUYaml
{
    [MenuItem("UYamlTest/Serialize")]
    static public void TestSerialize()
    {
        Object uAsset = Selection.activeObject;
        string pathOrg = Path.GetFullPath(Application.dataPath + "/../" + AssetDatabase.GetAssetPath(uAsset));
        UYamlAsset uYamlAsset = UYamlParser.ReadFromString(File.ReadAllText(pathOrg));
        string pathNew = pathOrg+".uyaml";
        File.WriteAllText(pathNew, UYamlParser.ToUYaml(uYamlAsset));
        Debug.LogFormat("DiffAsset done {0}", pathNew);
    }

    [MenuItem("UYamlTest/DiffAsset")]
    static public void DiffAsset()
    {
        string testPrefab = "UYaml/Editor/Test/Prefab/TestPrefab.prefab";
        string testPrefabVar = "UYaml/Editor/Test/Prefab/TestPrefabVar.prefab";
        string pathOrg = Path.GetFullPath(Application.dataPath + "/" + testPrefab);
        string pathVar = Path.GetFullPath(Application.dataPath + "/" + testPrefabVar);
        string diffFile = Path.GetFullPath(Application.dataPath + "/../TestPrefab.prefab.txt");
        Dictionary<string, string> dicDiff = UYamlUtil.DiffUAsset(pathOrg, pathVar);
        File.WriteAllText(diffFile, UYamlUtil.Dic2Str(dicDiff));
        Debug.LogFormat("DiffAsset done {0}", diffFile);
    }

    [MenuItem("UYamlTest/ApplyDiff")]
    static public void ApplyDiff()
    {
        string testPrefab = "UYaml/Editor/Test/Prefab/TestPrefab.prefab";
        string testPrfeabUYaml = "UYaml/Editor/Test/Prefab/TestPrefabUYaml.prefab";
        string pathOrg = Path.GetFullPath(Application.dataPath + "/" + testPrefab);
        string pathUYaml = Path.GetFullPath(Application.dataPath + "/" + testPrfeabUYaml);

        string diffFile = Path.GetFullPath(Application.dataPath + "/../TestPrefab.prefab.txt");
        if(!File.Exists(diffFile))
        {
            throw new System.Exception(string.Format("diff File not exist, run  UYamlTest->DiffAsset first! {0}", diffFile));
        }
        Dictionary<string, string> dicDiff = UYamlUtil.Str2Dic(File.ReadAllText(diffFile));
        UYamlAsset uYamlAsset = UYamlParser.ReadFromString(File.ReadAllText(pathOrg));
        UYamlUtil.ApplyDiff(uYamlAsset, dicDiff);
        File.WriteAllText(pathUYaml, UYamlParser.ToUYaml(uYamlAsset));
        AssetDatabase.Refresh();
        Debug.LogFormat("ApplyDiff done {0}", pathUYaml);
    }
}
