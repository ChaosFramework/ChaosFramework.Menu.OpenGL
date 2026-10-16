using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using ChaosFramework.Collections;
using ChaosFramework.IO;
using ChaosUtil.Platform.Paths;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Menu.OpenGl
{
    public class MalformedLanguageException(string msg, System.Exception ex = default)
        : System.Exception(msg, ex)
        ;

    public class LanguagePack
    {
        static readonly char[] LINE_BREAKS = ['\r', '\n'];

        static bool IdentifierEqual(string a, string b)
            => Normalization.NormalizeFullPath(a) == Normalization.NormalizeFullPath(b);

        static int IdentifierHashcode(string a)
            => a.Length == 0 ? 0 : char.ToLower(a[^1]);

        string NormalizeName(string path)
        {
            path = path.Remove(0, "lang/".Length);
            int endOfLangName = path.IndexOf('/');
            path = path.Remove(0, endOfLangName + 1);
            return path.Remove(path.Length - ".xml".Length);
        }

        public static void ProcessMacros(SysCol.IEnumerable<XmlDocument> docs)
        {
            SysCol.Dictionary<string, XmlNode> macros = [];
            foreach (XmlDocument doc in docs)
                foreach (XmlNode singleNode in doc.SelectNodes("//DefineMacro"))
                {
                    XmlAttribute idAttribute = singleNode.Attributes["id"]
                        ?? throw new MalformedLanguageException("Macro does not define an id");

                    if (!macros.ContainsKey(idAttribute.Value))
                        macros[idAttribute.Value] = singleNode;
                }

            foreach (XmlDocument doc in docs)
            {
                XmlNode singleNode;
                while ((singleNode = doc.SelectSingleNode("//Macro[not(contains(@class, 'DefineMacro'))]")) != null)
                {
                    XmlAttribute idAttribute = singleNode.Attributes["id"]
                        ?? throw new MalformedLanguageException("Macro does not define an id");

                    if (!macros.TryGetValue(idAttribute.Value, out XmlNode definingMacro))
                        throw new MalformedLanguageException($"Unknown Macro: {idAttribute.Value}");

                    SysCol.Dictionary<string, string> arguments = [];
                    foreach (XmlNode nod in definingMacro.SelectNodes("Arg"))
                        if (nod.Attributes["name"] is XmlAttribute nameAttr)
                        {
                            if (nod.Attributes["default"] is XmlAttribute valAttr)
                                arguments[nameAttr.Value] = valAttr.Value;
                            else
                                arguments[nameAttr.Value] = null;
                        }
                        else
                            throw new MalformedLanguageException("Arguments without names do not make that much sense.");

                    XmlNode argsNode = singleNode.SelectSingleNode("Args");
                    if (argsNode != null)
                        foreach (XmlAttribute attr in argsNode.Attributes)
                            arguments[attr.Name] = attr.Value;

                    XmlNode codeNode = definingMacro.SelectSingleNode("Code");
                    string processedInnerXml = codeNode == null ? "" : codeNode.InnerXml;
                    foreach (SysCol.KeyValuePair<string, string> arg in arguments)
                        processedInnerXml = arg.Value == null
                            ? throw new MalformedLanguageException($"No argument specified for parameter {arg.Key}")
                            : processedInnerXml.Replace($"{{{arg.Key}}}", arg.Value);

                    XmlNode parent = singleNode.ParentNode;
                    parent.RemoveChild(singleNode);
                    parent.InnerXml += processedInnerXml;
                }
            }
        }

        public static string EraseTabs(string src)
        {
            if (string.IsNullOrEmpty(src))
                return src;

            string[] lines = src.Trim(LINE_BREAKS).TrimEnd().Split('\n');
            int minTabs = int.MaxValue;
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim(LINE_BREAKS);

                if (lines[i].Trim() == "")
                    continue;

                int tabs = 0;
                foreach (char c in lines[i])
                    if (c == '\t')
                        tabs++;
                    else
                        break;

                minTabs = Min(tabs, minTabs);
            }

            System.Text.StringBuilder newStringBuilder = new(src.Length);
            for (int i = 0; i < lines.Length; i++)
                newStringBuilder.Append(lines[i][Min(lines[i].Length, minTabs)..])
                                .Append('\n');

            return newStringBuilder.ToString(0, newStringBuilder.Length - 1);
        }

        readonly string defaultLanguage;
        public readonly string name;
        readonly SysCol.Dictionary<string, XmlDocument[]> languageData
            = new(new GenericComparer<string>(IdentifierEqual, IdentifierHashcode));

        public LanguagePack(ChaosArchive archive, string name, string defaultLanguage)
        {
            // TODO: allow any StreamSource in order to support override files

            this.defaultLanguage = defaultLanguage;
            this.name = name;
            foreach (string identifier in EnumerateLanguageFiles(archive))
            {
                XmlDocument[] docs = [.. GetFiles(archive, identifier)];
                ProcessMacros(docs);
                languageData[identifier] = docs;
            }
        }

        SysCol.IEnumerable<string> EnumerateLanguageFiles(ChaosArchive archive)
        {
            foreach (string fileName in archive.GetFiles($"lang/{name}/*"))
                yield return NormalizeName(fileName);

            foreach (string fileName in archive.GetFiles($"lang/{defaultLanguage}/*"))
                yield return NormalizeName(fileName);
        }

        public SysCol.IEnumerable<XmlDocument> GetDocuments(string identifier)
            => languageData[identifier];

        SysCol.IEnumerable<XmlDocument> GetFiles(ChaosArchive archive, string identifier)
        {
            LinkedList<XmlDocument> files = [];
            string lang = name, langFile;
            while (archive.ContainsFile(langFile = $"lang/{lang}/{identifier}.xml") && lang != defaultLanguage)
            {
                XmlDocument itemDoc = new();

                using (Stream memStream = archive.OpenRead(langFile))
                    itemDoc.Load(memStream);
                yield return itemDoc;

                XmlNode rootNode = itemDoc.SelectSingleNode("*");
                XmlAttribute baseFile = rootNode.Attributes["baseFile"];
                if (baseFile == null)
                    break;
                lang = baseFile.Value;
            }
            XmlDocument defaultItemDoc = new();
            using (Stream memStream = archive.OpenRead($"lang/{defaultLanguage}/{identifier}.xml"))
                defaultItemDoc.Load(memStream);

            yield return defaultItemDoc;
        }

        public string GetText(string sourcePath)
        {
            string[] split = sourcePath.Split(['#'], 2);
            if (split.Length < 2)
                throw new ArgumentException($"{nameof(sourcePath)} must adhere to the format <file>#<xpath>", nameof(sourcePath));
            else
            {
                XmlNode node = GetNode(split[0].Trim(), split[1].Trim());
                if (node == null)
                    return null;
                else
                    return EraseTabs(node.InnerText);
            }
        }

        public string[] GetText(string file, string[] ids, string nodeType = "Text", bool normalizeTabs = true)
        {
            string[] output = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                XmlNode node = GetNode(file, $"Root/{nodeType}[@id='{ids[i]}']");
                output[i] = node == null ? "" : node.InnerText;
                if (normalizeTabs)
                    output[i] = EraseTabs(output[i]);
            }

            return output;
        }

        public string GetTextById(string file, string id, string nodeType = "Text", bool normalizeTabs = true)
            => GetTextByPath(file, $"Root/{nodeType}[@id='{id}']", normalizeTabs);

        public string GetTextByPath(string file, string path, bool normalizeTabs = true)
        {
            XmlNode node = GetNode(file, path);
            if (node == null)
                return null;

            return normalizeTabs ? EraseTabs(node.InnerText) : node.InnerText;
        }

        public SysCol.IEnumerable<string> EnumerateFiles(string pattern)
        {
            Regex regex = new(GlobRegex.ConvertGlobToRegex(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            foreach (SysCol.KeyValuePair<string, XmlDocument[]> file in languageData)
                if (regex.IsMatch(file.Key))
                    yield return file.Key;
        }

        public XmlNode GetNode(string file, string nodePath)
        {
            if (!languageData.TryGetValue(file, out XmlDocument[] sourceGroup))
                return null;

            foreach (XmlDocument doc in sourceGroup)
            {
                if (nodePath == null)
                    return doc.SelectSingleNode("*");

                XmlNode node = doc.SelectSingleNode(nodePath);
                if (node != null)
                    return node;
            }

            return null;
        }

        public LinkedList<XmlNode> GetNodes(string file, string nodePath)
        {
            LinkedList<XmlNode> output = [];
            if (!languageData.TryGetValue(file, out XmlDocument[] sourceGroup))
                return output;

            foreach (XmlDocument doc in sourceGroup)
                foreach (XmlNode singleNode in doc.SelectNodes(nodePath))
                    output.Add(singleNode);

            return output;
        }
    }
}
