/**************************************************************
 *  Filename:    UYamlNode.cs
 *  @author:     wuliangyu
 *  @version     2022-02-24
 **************************************************************/

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System;

namespace UYaml
{
    public class ChildNotFoundException : System.Exception 
    {
        public ChildNotFoundException(string info) : base(info){}
    }

    public class OperateNotSupportException : Exception
    {
        public OperateNotSupportException(string info) : base(info) { }
    }

    public class SequenceKeyIllvalidException : Exception
    {
        public SequenceKeyIllvalidException(string info) : base(info) { }
    }

    public enum UYamlNodeType
    {
        Scalar,
        Mapping,
        Sequence,
        Document
    }

    public class UYamlAsset
    {
        public string yamlInfo { get; set; }
        public string unityInfo { get; set; }

        public List<UYamlDoc> udocList = new List<UYamlDoc>();

        public UYamlDoc GetUYamlDocByAnchor(string anthor)
        {
            int i;
            UYamlDoc uYamlDoc = null;
            for (i=0; i< udocList.Count; ++i)
            {
                if(udocList[i].anchor == anthor)
                {
                    uYamlDoc = udocList[i];
                    break;
                }
            }
            return uYamlDoc;
        }

        public bool Apply(string path, string value)
        {
            string key;
            string subPath = UYamlParser.SplitPath(path, out key);
            UYamlDoc uYamlDoc = GetUYamlDocByAnchor(key);
            if(uYamlDoc == null)
            {
                return false;
            }
            else
            {
                return uYamlDoc.Apply(subPath, value);
            }
        }
    }

    public class UYamlDoc : UYamlNode
    {
        List<UYamlNode> _rootNode;

        public int uObjectType { get; set; }
        public string anchor { get; set; }
        public string ext { get; set; }
        public string uObjectName { get; set; }
        public UYamlNodeMapping rootNode { get; set; }

        public UYamlDoc()
        {
            rootNode = new UYamlNodeMapping();
            rootNode.parent = this;
        }

        public override UYamlNodeType NodeType
        {
            get { return UYamlNodeType.Document; }
        }

        public override string ToUYaml(string prefix = null)
        {
            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrEmpty(ext))
            {
                sb.Append(string.Format("--- !u!{0} &{1}", uObjectType, anchor));
            }
            else
            {
                sb.Append(string.Format("--- !u!{0} &{1} {2}", uObjectType, anchor, ext));
            }
            sb.Append(rootNode.ToUYaml());
            return sb.ToString();
        }

        public override UYamlNode GetChild(string path)
        {
            return rootNode.GetChild(path);
        }

        public override string GetPathForChild(UYamlNode node)
        {
            if(node == rootNode)
            {
                return anchor;
            }
            return string.Format("{0}/{1}", anchor, rootNode.GetPathForChild(node));
        }

        public override string GetPath()
        {
            return anchor;
        }

        public override bool Apply(string path, string value)
        {
            UYamlNode uYamlNode = rootNode.GetChild(path);
            if (uYamlNode == null)
            {
                return false;
            }
            if (uYamlNode.NodeType != UYamlNodeType.Scalar)
            {
                throw new System.Exception(string.Format("uyaml node type not supprt for apply value path:{0} type:{1}",
                        path, uYamlNode.NodeType));
            }
            ((UYamlNodeScalar)uYamlNode).value = value;
            return true;
        }
    }

    public abstract class UYamlNode
    {
        public UYamlNode parent { get; set; }
        public int level { get; set; }
        public bool isSingleLine { get; set; }

        public abstract UYamlNodeType NodeType{ get; }

        public abstract UYamlNode GetChild(string path);

        public abstract string GetPathForChild(UYamlNode node);

        public virtual string GetPath()
        {
            StringBuilder sb = new StringBuilder();
            UYamlNode uYamlNode = this;
            while (uYamlNode.parent != null)
            {
                sb.Insert(0,string.Format("{0}/",uYamlNode.parent.GetPathForChild(uYamlNode)));
                uYamlNode = uYamlNode.parent;
            }
            return Regex.Replace(sb.ToString(), "/$", "");
        }

        public virtual bool Apply(string path, string value)
        {
            string key;
            string subPath = UYamlParser.SplitPath(path, out key);
            UYamlNode uYamlNode = GetChild(path);
            if(uYamlNode == null)
            {
                return false;
            }
            if (uYamlNode.NodeType != UYamlNodeType.Scalar)
            {
                throw new System.Exception(string.Format("uyaml node not supprt for apply value {0}", path));
            }
            ((UYamlNodeScalar)uYamlNode).value = value;
            return true;
        }

        public abstract string ToUYaml(string prefix=null);
    }

    public class UYamlNodeScalar: UYamlNode
    {
        public UYamlNodeScalar() { }
        public UYamlNodeScalar(string val)
        {
            this.value = val;
            isSingleLine = true;
        }

        public override UYamlNodeType NodeType
        {
            get { return UYamlNodeType.Scalar;  }
        }

        public string value { get; set; }

        public override UYamlNode GetChild(string path)
        {
            throw new OperateNotSupportException("UYamlNodeScalar do not support GetChild");
        }

        public override string GetPathForChild(UYamlNode node)
        {
            throw new OperateNotSupportException("UYamlNodeScalar do not support GetPathForChild");
        }

        public override string ToUYaml(string prefix = null)
        {
            return value;
        }
    }

    public class UYamlNodeMapping: UYamlNode
    {
        private Dictionary<string, UYamlNode> _children = new Dictionary<string, UYamlNode>();

        public override UYamlNodeType NodeType
        {
            get { return UYamlNodeType.Mapping; }
        }

        public void AddChild(string key, UYamlNode uYamlNode)
        {
            uYamlNode.parent = this;
            _children.Add(key, uYamlNode);
        }

        public Dictionary<string, UYamlNode> children
        {
            get { return _children; }
        }

        public override UYamlNode GetChild(string path)
        {
            UYamlNode uYamlNode;
            string key;
            string subPath = UYamlParser.SplitPath(path, out key);
            if(!_children.TryGetValue(key, out uYamlNode))
            {
                return null;
            }
            if(string.IsNullOrEmpty(subPath))
            {
                return uYamlNode;
            }
            return uYamlNode.GetChild(subPath);
        }

        public override string GetPathForChild(UYamlNode node)
        {
            string key = null;
            foreach(KeyValuePair<string, UYamlNode> entry in _children)
            {
                if(entry.Value == node)
                {
                    key = entry.Key;
                    break;
                }
            }
            if(string.IsNullOrEmpty(key))
            {
                throw new ChildNotFoundException(node.ToString());
            }
            return key;
        }

        public override string ToUYaml(string prefix = null)
        {
            StringBuilder sb = new StringBuilder();
            string subPrefix = UYamlParser.GetLevelPrefix(level);
            if(isSingleLine)
            {
                sb.Append("{");
                bool isFirst = true;
                foreach (KeyValuePair<string, UYamlNode> entry in _children)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        sb.AppendFormat("{0}: {1}", entry.Key, entry.Value.ToUYaml());
                    }
                    else
                    {
                        sb.AppendFormat(", {0}: {1}", entry.Key, entry.Value.ToUYaml());
                    }
                }
                sb.Append("}");
            }
            else
            {
                foreach (KeyValuePair<string, UYamlNode> entry in _children)
                {
                    if (!entry.Value.isSingleLine)
                    {
                        sb.AppendFormat("\r\n{0}{1}:{2}", subPrefix, entry.Key, entry.Value.ToUYaml());
                    }
                    else
                    {
                        sb.AppendFormat("\r\n{0}{1}: {2}", subPrefix, entry.Key, entry.Value.ToUYaml());
                    }
                }
            }
            return sb.ToString();
        }
    }

    public class UYamlNodeSequence : UYamlNode
    {
        private List<UYamlNode> _children;

        public UYamlNodeSequence()
        {
            _children = new List<UYamlNode>();
        }

        public List<UYamlNode> Children
        {
            get { return _children; }
        }

        public UYamlNode GetChild(int idx)
        {
            return _children[idx];
        }

        public void AddChild(UYamlNode uYamlNode)
        {
            uYamlNode.parent = this;
            _children.Add(uYamlNode);
        }

        public override UYamlNodeType NodeType
        {
            get { return UYamlNodeType.Sequence; }
        }

        public override UYamlNode GetChild(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                return this;
            }

            string key;
            string subPath = UYamlParser.SplitPath(path, out key);

            Match match = Regex.Match(key, @"\$(\d+)");
            if(!match.Success)
            {
                throw new SequenceKeyIllvalidException(string.Format("illvalid key for UYamlNodeSequence {0}", key));
            }
            UYamlNode uYamlNode = GetChild(int.Parse(match.Groups[1].Value));
            if (string.IsNullOrEmpty(subPath))
            {
                return uYamlNode;
            }

            return uYamlNode.GetChild(subPath);
        }

        public override string GetPathForChild(UYamlNode node)
        {
            int idx = _children.IndexOf(node);
            if(idx==-1)
            {
                throw new ChildNotFoundException(node.ToString());
            }
            return string.Format("${0}", idx);
        }

        public override string ToUYaml(string prefix = null)
        {
            if(isSingleLine)
            {
                return "[]";
            }
            string subPrefix = UYamlParser.GetLevelPrefix(this.level, true);
            StringBuilder sb = new StringBuilder();
            int i;
            string itemContent;
            for (i=0; i<_children.Count; ++i)
            {
                itemContent = _children[i].ToUYaml();
                itemContent = Regex.Replace(itemContent, "^\r\n", "").TrimStart();
                sb.Append(string.Format("\r\n{0}{1}", subPrefix, itemContent));
            }
            return sb.ToString();
        }
    }
}
