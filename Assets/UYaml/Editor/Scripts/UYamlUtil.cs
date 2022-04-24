/**************************************************************
 *  Filename:    UYamlUtil.cs
 *  @author:     wuliangyu
 *  @version     2022-02-24
 **************************************************************/

using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace UYaml
{
    public class UYamlUtil
    {
        public delegate void ActionScalarNode(UYamlNodeScalar uYamlNodeScalar);

        static public Dictionary<string, string> CollectScalarValues(UYamlAsset uYamlAsset)
        {
            Dictionary<string, string> dicValues = new Dictionary<string, string>();
            int i;
            for(i=0; i< uYamlAsset.udocList.Count; ++i)
            {
                TraverseScalarNode(uYamlAsset.udocList[i], (UYamlNodeScalar uYamlNodeScalar)=>
                {
                    dicValues.Add(uYamlNodeScalar.GetPath(), uYamlNodeScalar.value);
                });
            }
            return dicValues;
        }

        static public Dictionary<string, string> DiffUAsset(string pathBase, string pathVar)
        {
            UYamlAsset uYamlAssetBase = UYamlParser.ReadFromString(File.ReadAllText(pathBase));
            UYamlAsset uYamlAssetVar = UYamlParser.ReadFromString(File.ReadAllText(pathVar));
            return UYamlUtil.DiffUAsset(uYamlAssetBase, uYamlAssetVar);
        }

        static public Dictionary<string, string> DiffUAsset(UYamlAsset uYamlAssetBase, UYamlAsset uYamlAssetVar)
        {
            Dictionary<string, string> valuesBase = CollectScalarValues(uYamlAssetBase);
            Dictionary<string, string> valuesVar = CollectScalarValues(uYamlAssetVar);
            Dictionary<string, string> valuesDiff = new Dictionary<string, string>();

            string value;
            foreach(KeyValuePair<string, string> entry in valuesVar)
            {
                if(valuesBase.TryGetValue(entry.Key, out value))
                {
                    if(!entry.Value.Equals(value))
                    {
                        valuesDiff.Add(entry.Key, entry.Value);
                    }
                }
            }

            return valuesDiff;
        }

        static public void ApplyDiff(UYamlAsset uYamlAsset, Dictionary<string, string> valuesDiff)
        {
            foreach (KeyValuePair<string, string> entry in valuesDiff)
            {
                uYamlAsset.Apply(entry.Key, entry.Value);
            }
        }

        static public string Dic2Str(Dictionary<string, string> valuesDiff)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in valuesDiff)
            {
                sb.AppendLine(string.Format("{0}:{1}", entry.Key, entry.Value));
            }
            return sb.ToString();
        }

        static public Dictionary<string, string> Str2Dic(string text)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            StringReader sr = new StringReader(text);
            string line;
            while(true)
            {
                line = sr.ReadLine();
                if(string.IsNullOrEmpty(line))
                {
                    break;
                }
                Match match = Regex.Match(line, @"([\w/]+):([\w-.]+)");
                if(match.Success)
                {
                    dic.Add(match.Groups[1].Value, match.Groups[2].Value);
                }
                else
                {
                    UnityEngine.Debug.LogFormat("get item failed {0}", line);
                }
            }
            return dic;
        }

        static public void TraverseScalarNode(UYamlNode uYamlNode, ActionScalarNode actionScalarNode)
        {
            UYamlNodeType uYamlNodeType = uYamlNode.NodeType;
            if (uYamlNodeType == UYamlNodeType.Scalar)
            {
                if(actionScalarNode != null)
                {
                    actionScalarNode((UYamlNodeScalar)uYamlNode);
                }
                return;
            }
            else if (uYamlNodeType == UYamlNodeType.Mapping)
            {
                UYamlNodeMapping uYamlNodeMapping = (UYamlNodeMapping)uYamlNode;
                foreach (KeyValuePair<string, UYamlNode> entry in uYamlNodeMapping.children)
                {
                    TraverseScalarNode(entry.Value, actionScalarNode);
                }
            }
            else if (uYamlNodeType == UYamlNodeType.Sequence)
            {
                UYamlNodeSequence uYamlNodeSequence = (UYamlNodeSequence)uYamlNode;
                int i;
                for (i = 0; i < uYamlNodeSequence.Children.Count; ++i)
                {
                    TraverseScalarNode(uYamlNodeSequence.GetChild(i), actionScalarNode);
                }
            }
            else if (uYamlNodeType == UYamlNodeType.Document)
            {
                TraverseScalarNode(((UYamlDoc)uYamlNode).rootNode, actionScalarNode);
            }
        }
    }
}
