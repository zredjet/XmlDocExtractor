using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class XmlDocExtractor
{
    /// <summary>
    /// Main
    /// </summary>
    /// <remarks>
    /// Usage: XmlDocExtractor source_file [-remarks]
    /// 　あああ
    /// </remarks>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: XmlDocExtractor <source_file> [-remarks]");
            return;
        }

        string sourceFile = args[0];
        bool extractRemarksOnly = args.Contains("-remarks");

        string code = File.ReadAllText(sourceFile);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var xmlDoc = method.GetLeadingTrivia()
                                .Select(trivia => trivia.GetStructure())
                                .OfType<DocumentationCommentTriviaSyntax>()
                                .FirstOrDefault();
            if (xmlDoc != null)
            {
                // XMLドキュメントをXMLとしてパース
                string xml = xmlDoc.ToFullString();
                // /// を削除
                xml = xml.Replace("///", "");
                // 特殊文字をエスケープ
                //xml = System.Security.SecurityElement.Escape(xml);
                // ルート要素でラップ
                xml = "<root>" + xml + "</root>";

                try
                {
                    XDocument doc = XDocument.Parse(xml);
                    // メソッド名を出力
                    Console.WriteLine($"メソッド名: {method.Identifier.Text}");

                    if (extractRemarksOnly)
                    {
                        // Remarksタグの内容のみ抽出
                        var remarks = doc.Root?.Element("remarks")?.Value;
                        if (!string.IsNullOrEmpty(remarks))
                        {
                            //remarks = remarks.Replace("\n", "").Replace("\r", "").Trim();
                            // 各行の先頭の空白を削除
                            var lines = remarks.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            remarks = string.Join("\n", lines.Select(line =>
                            {
                                // 全角スペースを保持しつつ、他の空白を削除
                                var trimmedLine = line.TrimStart();
                                if (line.StartsWith("　"))
                                {
                                    trimmedLine = "　" + trimmedLine;
                                }
                                return trimmedLine;
                            }));
                            Console.WriteLine($"Remarks:");
                            Console.WriteLine($"{remarks}");
                        }
                    }
                    else
                    {
                        // XML全体を出力
                        Console.WriteLine(doc.ToString());
                    }
                    Console.WriteLine();
                }
                catch (System.Xml.XmlException ex)
                {
                    Console.WriteLine($"XML Parse Error:{ex.Message}");
                    Console.WriteLine(xml);
                }
            }
        }
    }
}