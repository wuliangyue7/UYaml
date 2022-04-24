/**************************************************************
 *  Filename:    UYamlParser.cs
 *  @author:     wuliangyu
 *  @version     2022-02-24
 **************************************************************/

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace UYaml
{
    public class UYamlParser
    {
        static public UYamlAsset ReadFromString(string str)
        {
            UYamlAsset uYamlAsset = new UYamlAsset();
            str = str.Replace("\n\r", "\n").Replace("\r\n", "\n").Replace(",\n", ",");
            string[] lines = str.Split('\n');
            int i;
            List<string> lineList = new List<string>();
            string strLine = "";
            for (i = 0; i < lines.Length; ++i)
            {
                if(!string.IsNullOrEmpty(strLine))
                {
                    strLine = strLine + lines[i].TrimStart();
                }
                else
                {
                    strLine = lines[i];
                }
                if(!IsSingleLine(strLine))
                {
                    continue;
                }
                lineList.Add(strLine);
                strLine = "";
            }

            uYamlAsset.yamlInfo = lines[0];
            uYamlAsset.unityInfo = lines[1];
            string lineStr;
            UYamlDoc uYamlDoc;
            int consume;
            for (i = 2; i < lineList.Count; ++i)
            {
                lineStr = lines[i];
                if (lineStr.StartsWith("--- "))
                {
                    uYamlDoc = ParserRoot(lineStr);
                    uYamlAsset.udocList.Add(uYamlDoc);
                    KeyValuePair<string, UYamlNode> keyValuePair = ParserYamlNode(lineList, i+1, out consume);
                    uYamlDoc.rootNode.AddChild(keyValuePair.Key, keyValuePair.Value);
                    i += consume - 1;
                }
            }

            return uYamlAsset;
        }

        static public string[] FormatLine(string[] lines)
        {
            const int MaxLineChrNum = 80;
            List<string> strList = new List<string>();
            int i = 0;
            int strLen, startIdx, subLevel;
            string subPrefix;
            for(i=0;  i< lines.Length; ++i)
            {
                strLen = lines[i].Length;
                if (strLen <= MaxLineChrNum)
                {
                    strList.Add(lines[i]);
                }
                else
                {
                    int lastIdx = lines[i].IndexOf(',', MaxLineChrNum);
                    if (lastIdx == -1)
                    {
                        strList.Add(lines[i]);
                        continue;
                    }
                    else
                    {
                        strList.Add(lines[i].Substring(0, lastIdx+1));
                    }
                    startIdx = lastIdx+1;
                    subLevel = GetNodeLevel(lines[i]) + 2;
                    //only split 2 row now
                    subPrefix = GetLevelPrefix(subLevel);
                    strList.Add(string.Format("{0}{1}", subPrefix, lines[i].Substring(startIdx+1)));
                }
            }
            return strList.ToArray();
        }

        static public int GetNodeLevel(string str)
        {
            int i;
            for(i=0; i< str.Length; ++i)
            {
                if(str[i] != ' ' && str[i] != '-')
                {
                    break;
                }
            }
            
            return i;
        }

        static public string GetLevelPrefix(int level, bool isSeq = false)
        {
            StringBuilder sb = new StringBuilder();
            int i;
            for (i = 0; i < level; ++i)
            {
                if(isSeq && i== level-2)
                {
                    sb.Append("-");
                }
                else
                {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        static public bool IsSingleLine(string str)
        {
            int i;
            int countBL = 0;
            int countBR = 0;
            for(i=0; i< str.Length; ++i)
            {
                if(str[i] == '{')
                {
                    ++countBL;
                }
                else if(str[i] == '}')
                {
                    ++countBR;
                }
            }
            return countBL == countBR;
        }

        static public KeyValuePair<string, UYamlNode> ParserYamlNode(List<string> lineList, int startIdx, out int consumeLine)
        {
            int cost;
            consumeLine = 1;
            int chrIdx = lineList[startIdx].IndexOf(':');
            string key = lineList[startIdx].Substring(0, chrIdx).Replace("- ", "").TrimStart();
            string content = null;
            if(chrIdx < lineList[startIdx].Length-1)
            {
                content = lineList[startIdx].Substring(chrIdx + 1);
            }
            UYamlNode uYamlNode = null;
            if (!string.IsNullOrEmpty(content))
            {
                if (content.Equals("[]"))
                {
                    uYamlNode = ParserYamlNodeSequence(lineList, startIdx, out cost);
                }
                else if (content.TrimStart().StartsWith("{"))
                {
                    uYamlNode = ParserYamlNodeMappingShort(content);
                }
                else
                {
                    uYamlNode = ParserYamlNodeScalar(content);
                }
            }
            else
            {
                if (lineList.Count <= startIdx+1)
                {
                    throw new System.Exception(string.Format("unknow node type for {0}", lineList[startIdx]));
                }
                bool bSeq = lineList[startIdx + 1].TrimStart().StartsWith("- ");
                if(bSeq)
                {
                    uYamlNode = ParserYamlNodeSequence(lineList, startIdx+1, out cost);
                    consumeLine += cost;
                }
                else
                {
                    uYamlNode = ParserYamlNodeMappingMutiLine(lineList, startIdx+1, out cost);
                    consumeLine += cost;
                }
            }

            return new KeyValuePair<string, UYamlNode>(key, uYamlNode);
        }

        static public KeyValuePair<string, UYamlNode> parserSingleLine(string str)
        {
            KeyValuePair<string, UYamlNode> item;
            string key = "";
            UYamlNode uYamlNode = null;
            if(str.TrimStart().StartsWith("{"))
            {
                uYamlNode = ParserYamlNodeMappingShort(str);
                item = new KeyValuePair<string, UYamlNode>(key, uYamlNode);
            }
            else
            {
                int chrIdx = str.IndexOf(':');
                key = str.Substring(0, chrIdx);
                string content = null;
                if (chrIdx < str.Length - 1)
                {
                    content = str.Substring(chrIdx + 1);
                }
                if (!string.IsNullOrEmpty(content))
                {
                    if (content.StartsWith("["))
                    {
                        uYamlNode = ParserYamlNodeSequenceShort(content);
                    }
                    else if (content.TrimStart().StartsWith("{"))
                    {
                        uYamlNode = ParserYamlNodeMappingShort(content);
                    }
                    else
                    {
                        uYamlNode = ParserYamlNodeScalar(content);
                    }
                }
                item = new KeyValuePair<string, UYamlNode>(key, uYamlNode);
            }

            return item;
        }

        static public UYamlNodeScalar ParserYamlNodeScalar(string str)
        {
            return new UYamlNodeScalar(str.TrimStart());
        }

        static public UYamlNodeMapping ParserYamlNodeMappingShort(string str)
        {
            UYamlNodeMapping uYamlNodeMapping = new UYamlNodeMapping();
            uYamlNodeMapping.isSingleLine = true;
            MatchCollection mts = Regex.Matches(str, @"([\w.-]+): ([\w.-]+)");
            for (int i = 0; i < mts.Count; i++)
            {
                Match mt = mts[i];
                uYamlNodeMapping.AddChild(mt.Groups[1].Value, new UYamlNodeScalar(mt.Groups[2].Value));
            }

            return uYamlNodeMapping;
        }

        static public UYamlNodeSequence ParserYamlNodeSequenceShort(string str)
        {
            if(!str.Equals("[]"))
            {
                throw new System.Exception(string.Format("SequenceShort with value not implement! {0}", str));
            }
            UYamlNodeSequence uYamlNodeSequence = new UYamlNodeSequence();
            uYamlNodeSequence.isSingleLine = true;
            return uYamlNodeSequence;
        }

        static public UYamlNodeMapping ParserYamlNodeMappingMutiLine(List<string> lineList, int startIdx, out int consumeLine)
        {
            consumeLine = 0;
            UYamlNodeMapping uYamlNodeMapping = new UYamlNodeMapping();
            uYamlNodeMapping.isSingleLine = false;
            uYamlNodeMapping.level = GetNodeLevel(lineList[startIdx]);
            int cost = 0;
            int idx = startIdx;
            while(true)
            {
                if(idx >= lineList.Count || lineList[idx].StartsWith("---"))
                {
                    break;
                }
                if (GetNodeLevel(lineList[idx]) < uYamlNodeMapping.level || idx >= lineList.Count)
                {
                    break;
                }

                //new item for Sequence
                if (idx != startIdx && lineList[idx].TrimStart().StartsWith("- "))
                {
                    break;
                }

                KeyValuePair<string, UYamlNode> item = ParserYamlNode(lineList, idx, out cost);
                uYamlNodeMapping.AddChild(item.Key, item.Value);
                idx += cost;
            }
            consumeLine = idx - startIdx;

            return uYamlNodeMapping;
        }

        static public UYamlNodeSequence ParserYamlNodeSequence(List<string> lineList, int startIdx, out int consumeLine)
        {
            UYamlNodeSequence uYamlNodeSequence = new UYamlNodeSequence();
            uYamlNodeSequence.isSingleLine = false;
            uYamlNodeSequence.level = GetNodeLevel(lineList[startIdx]);
            consumeLine = 0;
            int i;
            int cost;
            string content;
            UYamlNodeMapping uYamlNodeMapping = null;
            for (i=startIdx; i< lineList.Count; ++i)
            {
                if(lineList[i].TrimStart().StartsWith("- "))
                {
                    content = lineList[i].TrimStart().Substring(1).TrimStart();
                    if(content.StartsWith("{"))
                    {
                        uYamlNodeSequence.AddChild(ParserYamlNodeMappingShort(content));
                    }
                    else
                    {
                        uYamlNodeMapping = ParserYamlNodeMappingMutiLine(lineList, i, out cost);
                        uYamlNodeSequence.AddChild(uYamlNodeMapping);
                        i += cost - 1;
                    }
                }
                else
                {
                    break;
                }
            }
            consumeLine = i - startIdx;
            return uYamlNodeSequence;
        }

        static public UYamlDoc ParserRoot(string line)
        {
            string[] strs = line.Split(' ');
            UYamlDoc uYamlDoc = new UYamlDoc();
            uYamlDoc.uObjectType = int.Parse(strs[1].Substring(3));
            uYamlDoc.anchor = strs[2].Substring(1);
            if (strs.Length > 3)
            {
                uYamlDoc.ext = strs[3];
            }
            return uYamlDoc;
        }

        static public string ToUYaml(UYamlAsset uYamlAsset)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(uYamlAsset.yamlInfo);
            sb.AppendLine(uYamlAsset.unityInfo);

            int i;
            for(i=0; i< uYamlAsset.udocList.Count; ++i)
            {
                sb.AppendLine(uYamlAsset.udocList[i].ToUYaml());
            }
            string str = sb.ToString();
            str = str.Replace("\n\r", "\n").Replace("\r\n", "\n");
            string[] lines = str.Split('\n');
            lines = FormatLine(lines);
            sb = new StringBuilder();
            for (i=0; i< lines.Length; ++i)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    sb.AppendLine(lines[i]);
                }
            }
            return sb.ToString();
        }

        static public string SplitPath(string path, out string key)
        {
            string subPath = null;
            path = Regex.Replace(path, "/$", "");
            int idx = path.IndexOf("/");
            if(idx != -1)
            {
                key = path.Substring(0, idx);
                subPath = path.Substring(idx+1);
            }
            else
            {
                key = path;
            }
            return subPath;
        }
    }
}
