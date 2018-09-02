using AngleSharp;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

public static class PdfTextExtract
{
    //static PdfReader reader;
    //static string path;
    private static string flag = "";

    private static List<data> dataMS = new List<data>();

    public static string pdfText(string path)
    {
        PdfReader reader = new PdfReader(path);
        StringBuilder text = new StringBuilder();
        string t = "";
        int pages = 5;
        while (t.IndexOf("total for question", StringComparison.CurrentCultureIgnoreCase) < 0)
        {
            t = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, pages);
            pages++;
        }
        for (int page = pages - 2; page <= reader.NumberOfPages; page++)
        {
            t = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page);

            text.Append(t);
        }
        reader.Close();
        return text.ToString();
    }

    public static string questionSmall(string content)
    {
        string s = "";
        if (content.Contains("(iv)"))
        {
            s = "(iv)";
            //regex = @"(\(i\))(?s)(.*)\(([1-9])\).*(\(ii\))(?s)(.*)\(([1-9])\).*(\(iii\))(?s)(.*)\(([1-9]).*(\(iv\))(?s)(.*)\(([1-9])";
        }
        else if (content.Contains("(iii)"))
        {
            s = "(iii)";
            //   regex = @"(\(i\))(?s)(.*)\(([1-9])\).*(\(ii\))(?s)(.*)\(([1-9])\).*(\(iii\))(?s)(.*)\(([1-9])";
        }
        else if (content.Contains("(ii)"))
        {
            s = "(ii)";
            //   regex = @"(\(i\))(?s)(.*)\(([1-9])\).*(\(ii\))(?s)(.*)\(([1-9])\)";
        }
        else if (content.Contains("(i)"))
        {
            s = "(i)";
            //regex = @"(\(i\))(?s)(.*)\(([1-9])\)";
        }
        return s;
    }

    public static string dumpMarkScheme(string path)
    {
        path = path.Replace("QP", "MS");
        var text = pdfText(path).Split('\n').Where(x => !string.IsNullOrWhiteSpace(x.ToString()) && !string.IsNullOrEmpty(x.ToString())).ToList();
        //   var text1 = pdfText(path).Split('\n').Select(x => x.ToString()).First();
        StringBuilder ms = new StringBuilder();
        int i = 0;
        int beginIndex = 0, endIndex = 0;
        bool flag = false;
        string tempNo = "";
        string questionNo = "", line = "";
        for (i = 0; i <= text.Count() - 1; i++)
        {
            line = text[i];
            if (line.Contains("BLANK PAGE")) continue;
            line = !line.Contains("*") ? line.Substring(0, line.Length / 3) : line;
            if (line.IndexOf("(a)") > 0 || line.IndexOf("(b)") > 0 || line.IndexOf("(c)") > 0 || line.IndexOf("(d)") > 0 || line.IndexOf("(Total for question)", StringComparison.CurrentCultureIgnoreCase) > 0 || Regex.IsMatch(line, @"([1-9]|1[0-9]|2[0-4])([a-d])", RegexOptions.ExplicitCapture) || Regex.IsMatch(line, @"(1[1-9]|2[0-4])", RegexOptions.ExplicitCapture) || Regex.IsMatch(line, @"\*(1[1-9]|2[0-4])", RegexOptions.ExplicitCapture))
            {
                //begin markscheme
                //Debug.Print(line);
                // Debug.Print(flag.ToString());

                if (flag)
                {
                    endIndex = i;
                    endIndex--;
                    for (int j = beginIndex + 1; j < endIndex; j++)
                    {
                        if (!string.IsNullOrWhiteSpace(text[j]))
                            ms.AppendLine(text[j]);
                    }
                    if (!string.IsNullOrEmpty(ms.ToString()))
                    {
                        var question = text[beginIndex];
                        try
                        {
                            question = question.Substring(0, 9).Replace(" ", "");
                        }
                        catch { question.Substring(0, question.Length / 2).Replace(" ", ""); }
                        //   Debug.Print(line);
                        Match m;

                        if (question.Contains("(i)") | question.Contains("(ii)") || question.Contains("(iii)") || question.Contains("(iv)"))
                        {
                            m = Regex.Match(question, @"(1[0-9]|2[0-9]|24)\(([a-d])\)", RegexOptions.IgnorePatternWhitespace);
                            if (question.Contains("(a)"))
                            {
                                questionNo = m.Groups[1].Value.ToString();
                                tempNo = questionNo;
                            }
                            else
                            {
                                questionNo = tempNo;
                            }
                            dataMS.Add(new data()
                            {
                                questionNo = questionNo,
                                questionAqua = m.Groups[2].Value.ToString(),
                                questionSmall = questionSmall(question),
                                markScheme = ms.ToString(),
                            });
                        }
                        else
                        {
                            m = Regex.Match(question, @"([1-9]|1[0-9]|2[0-4])\(([a-d])\)", RegexOptions.IgnorePatternWhitespace);
                            if (string.IsNullOrEmpty(m.Groups[0].Value))
                            {
                                m = Regex.Match(question, "([1-9]|1[0-9]|2[0-4])([a-d])");
                                questionNo = m.Groups[1].Value.ToString();
                            }
                            if (question.Contains("(a)") || question[2] == 'a')
                            {
                                questionNo = m.Groups[1].Value.ToString();
                                tempNo = questionNo;
                            }
                            else if (question.Contains("*"))
                            {
                                var r = Regex.Match(question, @"\*(1[1-9]|2[0-4])");
                                questionNo = r.Groups[1].Value.ToString();
                            }
                            else if (string.IsNullOrWhiteSpace(questionNo))
                            {
                                var r = Regex.Match(question, "(1[1-9]|2[0-4])");
                                questionNo = r.Groups[1].Value.ToString();
                            }
                            else
                            {
                                questionNo = tempNo;
                            }
                            if (questionNo.Length < 2)
                            {
                                questionNo = "1" + questionNo;
                            }

                            dataMS.Add(new data()
                            {
                                questionNo = questionNo,
                                questionAqua = m.Groups[2].Value.ToString(),
                                markScheme = ms.ToString(),
                            });
                        }
                    }
                    ms.Clear();
                    beginIndex = !Regex.IsMatch(line, @"(1[1-9]|2[0-4])", RegexOptions.ExplicitCapture) ? endIndex : i;
                    flag = false;
                    i--;
                }
                else
                {
                    beginIndex = i;
                    flag = true;
                }
            }
            //   i++;
        }
        return "";
        //   return text;
    }

    public static string dumpQuestions(string path)
    {
        PdfTextExtract.dumpMarkScheme(path.Replace("QP", "MS"));
        var list = dataMS;
        PdfReader reader = new PdfReader(path);
        StringBuilder text = new StringBuilder();
        string t = "";
        int questionNo = 0;
        int tempNo = 0;
        int pages = 3;
        Debug.Print(path.Substring(0, 4));
        text.AppendLine("questionNo,questionAqua,questionSmall,question,markScheme,score,Topics");
        while (t.IndexOf("section", StringComparison.CurrentCultureIgnoreCase) < 0 || t.IndexOf("answer all", StringComparison.CurrentCultureIgnoreCase) < 0)
        {
            if (pages >= 20) return "";
            t = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, pages);
            pages++;
        }

        for (int page = pages - 1; page <= reader.NumberOfPages; page++)
        {
            t = iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, page);
            if (t.Contains("formulae") || t.Contains("BLANK PAGE")) continue;
            t = Regex.Replace(t, @"(\\n\*.*\*\\n)", @"");

            string temp = t.Substring(0, 30);
            try
            {
                //www\.dynamicpapers\.com\\n(1[0-9]|2[0-9]|24)
                //  var r = Regex.Match(temp, @"((1[0-9]|2[0-9]|24))(?s).*\)");
                var r = Regex.Match(temp, @"(1[0-9]|2[0-9]|24)");
                questionNo = int.Parse(r.Groups[1].Value.ToString());
                //  System.Diagnostics.Debug.Print(questionNo.ToString());
            }
            catch
            {
                if (t.IndexOf("section", StringComparison.CurrentCultureIgnoreCase) < 0 || t.IndexOf("answer all", StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    if (!temp.Contains("(i)") || !temp.Contains("ii") || !temp.Contains("(b)") || !temp.Contains("(c)") || !temp.Contains("(d)"))
                    {
                        try
                        {
                            var r = Regex.Match(temp, @"((1[0-9]|2[0-9]|24))(?s).*[\\n\*.*\*\\n]");
                            questionNo = int.Parse(r.Groups[1].Value.ToString());
                            if (questionNo - tempNo >= 2 && tempNo != 0)
                            {
                                questionNo = ++tempNo;
                            }
                        }
                        catch
                        {
                            questionNo = tempNo;
                        }
                    }
                    else
                    {
                        questionNo = tempNo;
                    }
                }
                else
                {
                    var r = Regex.Match(t, @"((1[0-9]|2[0-9]|24))(?s).*[\\n\*.*\*\\n]");
                    questionNo = int.Parse(r.Groups[1].Value.ToString());
                }
            }

            //  if (tempNo > questionNo && questionNo-tempNo <= 2)
            //  {
            //      questionNo = tempNo;
            //    }

            tempNo = questionNo;
            foreach (var q in getData(questionNo, t))
            {
                text.AppendLine(q.ToString());
            }
        }
        reader.Close();
        return text.ToString();
    }

    public static string selectMarkScheme(string questionNo, string questionAqua = null, string questionSmall = null)
    {
        //foreach(data d in dataMS)
        //{
        //Debug.Print(d.ToString());
        //}
        if (questionAqua == null && questionSmall == null)
        {
            return dataMS.First(x => x.questionNo == questionNo).markScheme;
        }
        return dataMS.FirstOrDefault(x => x.questionNo == questionNo && x.questionSmall == questionSmall && x.questionAqua == questionAqua).markScheme;
    }

    public static List<data> getData(int no, string content)
    {
        List<data> datas = new List<data>();
        // string flag ="";
        if (no <= 10) return datas;
        if (Regex.IsMatch(content, @"\(([a-h])\)(?s)(.*?)\(([0-9])\)"))
        {
            var ms = Regex.Matches(content, @"\(([a-h])\)(?s)(.*?)\(([0-9])\)");
            foreach (Match g in ms)
            {
                var t = g.Groups[2].Value.ToString();
                if (!t.Contains("(i)") && !string.IsNullOrEmpty(t) && !string.IsNullOrWhiteSpace(t))
                {
                    var questionNo = no.ToString();
                    var questionAqua = g.Groups[1].Value.ToString();
                    datas.Add(new data()
                    {
                        questionNo = questionNo,
                        questionAqua = questionAqua,
                        question = t,
                        markScheme = selectMarkScheme(questionNo, questionAqua),
                        score = int.Parse(g.Groups[3].Value.ToString()),
                    });
                }
                else
                {
                    flag = g.Groups[1].Value.ToString();
                }
            }
        }
        else
        {
            var ms = Regex.Matches(content, no.ToString() + @"(.*)");
            foreach (Match g in ms)
            {
                var t = g.Groups[0].Value.ToString();
                if (!t.Contains("(i)") && !string.IsNullOrEmpty(t) && !string.IsNullOrWhiteSpace(t))
                {
                    var questionNo = no.ToString();
                    //        var questionAqua = g.Groups[1].Value.ToString();

                    datas.Add(new data()
                    {
                        questionNo = questionNo,
                        questionAqua = null,
                        question = t.Replace(".", string.Empty).Replace(@"\n", string.Empty),
                        markScheme = selectMarkScheme(questionNo),
                        score = 3,
                    });
                }
                else
                {
                    flag = g.Groups[1].Value.ToString();
                }
            }
        }
        //     reader = new PdfReader(path);
        if (content.Contains("(i)") || content.Contains("(ii)") || content.Contains("(iii)") || content.Contains("(iv)"))
        {
            try
            {
                string regex = "";
                //method 1
                if (content.Contains("(iv)"))
                {
                    regex = @"(\(i\))(?s)(.*)\(([1-9])\).*(\(ii\))(?s)(.*)\(([1-9])\).*(\(iii\))(?s)(.*)\(([1-9]).*(\(iv\))(?s)(.*)\(([1-9])";
                }
                else if (content.Contains("(iii)"))
                {
                    regex = @"(\(i\))(?s)(.*)\(([1-9])\).*(\(ii\))(?s)(.*)\(([1-9])\).*(\(iii\))(?s)(.*)\(([1-9])";
                }
                else if (content.Contains("(ii)"))
                {
                    regex = @"(\(i\))(?s)(.*)\(([1-9])\).*(\(ii\))(?s)(.*)\(([1-9])\)";
                }
                else if (content.Contains("(i)"))
                {
                    regex = @"(\(i\))(?s)(.*)\(([1-9])\)";
                }
                var ms1 = Regex.Matches(content, regex);
                string t = "";

                for (int i = 1; i <= ms1[0].Groups.Count; i += 3)
                {
                    t = ms1[0].Groups[i + 1].Value.ToString();
                    if (!string.IsNullOrEmpty(t) && !string.IsNullOrWhiteSpace(t))
                    {
                        var questionSmall = ms1[0].Groups[i].Value.ToString();
                        var questionNo = no.ToString();

                        datas.Add(new data()
                        {
                            questionNo = no.ToString(),
                            questionAqua = flag,
                            questionSmall = questionSmall,
                            question = t,
                            markScheme = selectMarkScheme(questionNo, flag, questionSmall),
                            score = int.Parse(ms1[0].Groups[i + 2].Value.ToString()),
                        });
                    }
                }
            }
            catch { }
        }

        //normal regex

        return datas;
    }

    public class data
    {
        public string questionNo { get; set; }

        public string questionAqua { get; set; }
        public string questionSmall { get; set; }
        public string question { get; set; }
        public string markScheme { get; set; }
        public string Topics { get; set; }
        public int score { get; set; }

        public override string ToString()
        {
            var t = @"""" + question + @"""";
            var t2 = @"""" + markScheme + @"""";
            return $"{questionNo},{questionAqua},{questionSmall},{t},{t2},{score},{Topics}";
        }
    }
}

public static class Updator
{
    public static void Download(string url = "http://www.physicsandmathstutor.com/past-papers/a-level-physics/edexcel-unit-1/")
    {
        var config = Configuration.Default.WithDefaultLoader();
        var dom = BrowsingContext.New(config).OpenAsync(url).Result;
        var files = dom.QuerySelectorAll("#post-62 > div.post-entry > ul > li").Select(x => x.GetElementsByTagName("a"));
        foreach (var node in files)
        {
            var name = node.First().TextContent + ".pdf";
            if (name.Contains("Combined") || name.Contains("Grade")) continue;
            var link = new Uri(node.First().GetAttribute("href"));
            new WebClient().DownloadFileAsync(link, name);
            //download
        }
    }
}

namespace IGCSE_BIO
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //   Updator.Download();
            //  string path  =  ("2012JUN_Q.pdf");
            /*   foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.pdf"))
               {
                   if (path.Contains("MS")) continue;
                   var t = PdfTextExtract.dumpQuestions(path);
                   Dispatcher.BeginInvoke(new Action(() =>
                   {
                       tb.Text = t; 
                   }));

                   File.WriteAllText(path + ".csv", t);
               }*/
         
            File.WriteAllText("January 2009 MS - Unit 1 Edexcel Physics A-level" + ".txt", PdfTextExtract.pdfText("January 2009 MS - Unit 1 Edexcel Physics A-level.pdf"));
            //     string path = ("January 2009 QP - Unit 1 Edexcel Physics A-level.pdf");
            //     var t = PdfTextExtract.dumpQuestions(path);

            //     tb.Text= t;
            //     File.WriteAllText(path + ".csv", t);
        }
    }
}