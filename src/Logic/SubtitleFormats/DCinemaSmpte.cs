﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Nikse.SubtitleEdit.Logic.SubtitleFormats
{
    class DCinemaSmpte : SubtitleFormat
    {
        //<?xml version="1.0" encoding="UTF-8"?>
        //<dcst:SubtitleReel xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:dcst="http://www.smpte-ra.org/schemas/428-7/2010/DCST"> 
        //  <Id>urn:uuid:7be835a3-cfb4-43d0-bb4b-f0b4c95e962e</Id>
        //  <ContentTitleText>2001, A Space Odissey</ContentTitleText> 
        //  <AnnotationText>This is a subtitle file</AnnotationText>
        //  <IssueDate>2012-06-26T12:33:59.000-00:00</IssueDate> 
        //  <ReelNumber>1</ReelNumber> 
        //  <Language>fr</Language>
        //  <EditRate>25 1</EditRate>
        //  <TimeCodeRate>25</TimeCodeRate>
        //  <StartTime>00:00:00:00</StartTime> 
        //  <LoadFont ID="theFontId">urn:uuid:3dec6dc0-39d0-498d-97d0-928d2eb78391</LoadFont>
        //  <SubtitleList
        //      <Font ID="theFontId" Size="39" Weight="normal" Color="FFFFFFFF">
        //      <Subtitle FadeDownTime="00:00:00:00" FadeUpTime="00:00:00:00" TimeOut="00:00:00:01" TimeIn="00:00:00:00" SpotNumber="1">
        //          <Text Vposition="10.0" Valign="bottom">Hallo</Text> 
        //      </Subtitle>
        //  </SubtitleList
        //</dcst:SubtitleReel>

        private double frameRate = 24;

        public override string Extension
        {
            get { return ".xml"; }
        }

        public override string Name
        {
            get { return "D-Cinema smpte"; }
        }

        public override bool HasLineNumber
        {
            get { return true; }
        }

        public override bool IsTimeBased
        {
            get { return true; }
        }

        public override bool IsMine(List<string> lines, string fileName)
        {
            var sb = new StringBuilder();
            lines.ForEach(line => sb.AppendLine(line));
            string xmlAsString = sb.ToString().Trim();
            if (xmlAsString.Contains("<dcst:SubtitleReel"))
            {
                var xml = new XmlDocument();
                try
                {
                    xmlAsString = xmlAsString.Replace("<dcst:", "<").Replace("</dcst:", "</");
                    xml.LoadXml(xmlAsString);
                    var subtitles = xml.DocumentElement.SelectNodes("//Subtitle");
                    if (subtitles != null && subtitles.Count >= 0)                    
                        return subtitles != null && subtitles.Count > 0;
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private static string RemoveSubStationAlphaFormatting(string s)
        {
            int indexOfBegin = s.IndexOf("{", StringComparison.Ordinal);
            while (indexOfBegin >= 0 && s.IndexOf("}", StringComparison.Ordinal) > indexOfBegin)
            {
                int indexOfEnd = s.IndexOf("}", StringComparison.Ordinal);
                s = s.Remove(indexOfBegin, (indexOfEnd - indexOfBegin) + 1);
                indexOfBegin = s.IndexOf("{", StringComparison.Ordinal);
            }
            return s;
        }

        public override string ToText(Subtitle subtitle, string title)
        {   
            var ss = Configuration.Settings.SubtitleSettings;

            if (!string.IsNullOrEmpty(ss.CurrentDCinemaEditRate))
            {
                string[] temp = ss.CurrentDCinemaEditRate.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                double d1, d2;
                if (temp.Length == 2 && double.TryParse(temp[0], out d1) && double.TryParse(temp[1], out d2))
                    frameRate = d1 / d2;
            }

            string xmlStructure =
                "<dcst:SubtitleReel Version=\"1.0\" xmlns:dcst=\"http://www.smpte-ra.org/schemas/428-7/2010/DCST\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" + Environment.NewLine +
                "  <dcst:Id>urn:uuid:7be835a3-cfb4-43d0-bb4b-f0b4c95e962e</dcst:Id>" + Environment.NewLine +
                "  <dcst:ContentTitleText></dcst:ContentTitleText> " + Environment.NewLine +
                "  <dcst:AnnotationText>This is a subtitle file</dcst:AnnotationText>" + Environment.NewLine +
                "  <dcst:IssueDate>2012-06-26T12:33:59.000-00:00</dcst:IssueDate>" + Environment.NewLine +
                "  <dcst:ReelNumber>1</dcst:ReelNumber>" + Environment.NewLine +
                "  <dcst:Language>en</dcst:Language>" + Environment.NewLine +
                "  <dcst:EditRate>25 1</dcst:EditRate>" + Environment.NewLine +
                "  <dcst:TimeCodeRate>25</dcst:TimeCodeRate>" + Environment.NewLine +
                "  <dcst:StartTime>00:00:00:00</dcst:StartTime> " + Environment.NewLine +
                "  <dcst:LoadFont ID=\"theFontId\">urn:uuid:3dec6dc0-39d0-498d-97d0-928d2eb78391</dcst:LoadFont>" + Environment.NewLine +
                "  <dcst:SubtitleList>" + Environment.NewLine +
                "    <dcst:Font ID=\"theFontId\" Size=\"39\" Weight=\"normal\" Color=\"FFFFFFFF\" Effect=\"border\" EffectColor=\"FF000000\">" + Environment.NewLine +
                "    </dcst:Font>" + Environment.NewLine +
                "  </dcst:SubtitleList>" + Environment.NewLine +
                "</dcst:SubtitleReel>";
                       
            var xml = new XmlDocument();
            xml.LoadXml(xmlStructure);
            var nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("dcst", xml.DocumentElement.NamespaceURI);

            if (string.IsNullOrEmpty(ss.CurrentDCinemaMovieTitle))
                ss.CurrentDCinemaMovieTitle = title;
            xml.DocumentElement.SelectSingleNode("dcst:ContentTitleText", nsmgr).InnerText = ss.CurrentDCinemaMovieTitle;
            xml.DocumentElement.SelectSingleNode("dcst:Id", nsmgr).InnerText = ss.CurrentDCinemaSubtitleId;
            xml.DocumentElement.SelectSingleNode("dcst:ReelNumber", nsmgr).InnerText = ss.CurrentDCinemaReelNumber;
            xml.DocumentElement.SelectSingleNode("dcst:IssueDate", nsmgr).InnerText = ss.CurrentDCinemaIssueDate;
            xml.DocumentElement.SelectSingleNode("dcst:Language", nsmgr).InnerText = ss.CurrentDCinemaLanguage;
            xml.DocumentElement.SelectSingleNode("dcst:EditRate", nsmgr).InnerText = ss.CurrentDCinemaEditRate;
            xml.DocumentElement.SelectSingleNode("dcst:LoadFont", nsmgr).InnerText = ss.CurrentDCinemaFontUri;
            int fontSize = ss.CurrentDCinemaFontSize;
            string loadedFontId = "Font1";
            if (!string.IsNullOrEmpty(ss.CurrentDCinemaFontId))
                loadedFontId = ss.CurrentDCinemaFontId;
            xml.DocumentElement.SelectSingleNode("dcst:SubtitleList/dcst:Font", nsmgr).Attributes["Size"].Value = fontSize.ToString();
            xml.DocumentElement.SelectSingleNode("dcst:SubtitleList/dcst:Font", nsmgr).Attributes["Color"].Value = "FF" + Utilities.ColorToHex(ss.CurrentDCinemaFontColor).TrimStart('#').ToUpper();
            xml.DocumentElement.SelectSingleNode("dcst:SubtitleList/dcst:Font", nsmgr).Attributes["ID"].Value = loadedFontId;
            xml.DocumentElement.SelectSingleNode("dcst:SubtitleList/dcst:Font", nsmgr).Attributes["Effect"].Value = ss.CurrentDCinemaFontEffect;
            xml.DocumentElement.SelectSingleNode("dcst:SubtitleList/dcst:Font", nsmgr).Attributes["EffectColor"].Value = "FF" + Utilities.ColorToHex(ss.CurrentDCinemaFontEffectColor).TrimStart('#').ToUpper();

            XmlNode mainListFont = xml.DocumentElement.SelectSingleNode("dcst:SubtitleList/dcst:Font", nsmgr);
            int no = 0;
            foreach (Paragraph p in subtitle.Paragraphs)
            {
                if (!string.IsNullOrEmpty(p.Text))
                {
                    XmlNode subNode = xml.CreateElement("dcst:Subtitle", "dcst");

                    XmlAttribute id = xml.CreateAttribute("SpotNumber");
                    id.InnerText = (no + 1).ToString();
                    subNode.Attributes.Append(id);

                    XmlAttribute fadeUpTime = xml.CreateAttribute("FadeUpTime");
                    fadeUpTime.InnerText = "00:00:00:00"; //Configuration.Settings.SubtitleSettings.DCinemaFadeUpDownTime.ToString();
                    subNode.Attributes.Append(fadeUpTime);

                    XmlAttribute fadeDownTime = xml.CreateAttribute("FadeDownTime");
                    fadeDownTime.InnerText = "00:00:00:00"; //Configuration.Settings.SubtitleSettings.DCinemaFadeUpDownTime.ToString();
                    subNode.Attributes.Append(fadeDownTime);

                    XmlAttribute start = xml.CreateAttribute("TimeIn");
                    start.InnerText = ConvertToTimeString(p.StartTime);
                    subNode.Attributes.Append(start);

                    XmlAttribute end = xml.CreateAttribute("TimeOut");
                    end.InnerText = ConvertToTimeString(p.EndTime);
                    subNode.Attributes.Append(end);


                    bool alignLeft = p.Text.StartsWith("{\\a1}") || p.Text.StartsWith("{\\a5}") || p.Text.StartsWith("{\\a9}") || // sub station alpha
                                    p.Text.StartsWith("{\\an1}") || p.Text.StartsWith("{\\an4}") || p.Text.StartsWith("{\\an7}"); // advanced sub station alpha

                    bool alignRight = p.Text.StartsWith("{\\a3}") || p.Text.StartsWith("{\\a7}") || p.Text.StartsWith("{\\a11}") || // sub station alpha
                                      p.Text.StartsWith("{\\an3}") || p.Text.StartsWith("{\\an6}") || p.Text.StartsWith("{\\an9}"); // advanced sub station alpha

                    bool alignVTop = p.Text.StartsWith("{\\a5}") || p.Text.StartsWith("{\\a6}") || p.Text.StartsWith("{\\a7}") || // sub station alpha
                                    p.Text.StartsWith("{\\an7}") || p.Text.StartsWith("{\\an8}") || p.Text.StartsWith("{\\an9}"); // advanced sub station alpha

                    bool alignVCenter = p.Text.StartsWith("{\\a9}") || p.Text.StartsWith("{\\a10}") || p.Text.StartsWith("{\\a11}") || // sub station alpha
                                      p.Text.StartsWith("{\\an4}") || p.Text.StartsWith("{\\an5}") || p.Text.StartsWith("{\\an6}"); // advanced sub station alpha

                    // remove styles for display text (except italic)
                    string text = RemoveSubStationAlphaFormatting(p.Text);


                    string[] lines = text.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    int vPos = 1 + lines.Length * 7;
                    int vPosFactor = (int)Math.Round(fontSize / 7.4);
                    if (alignVTop)
                    {
                        vPos = Configuration.Settings.SubtitleSettings.DCinemaBottomMargin; // Bottom margin is normally 8
                    }
                    else if (alignVCenter)
                    {
                        vPos = (int)Math.Round((lines.Length * vPosFactor * -1) / 2.0);
                    }
                    else
                    {
                        vPos = (lines.Length * vPosFactor) - vPosFactor + Configuration.Settings.SubtitleSettings.DCinemaBottomMargin; // Bottom margin is normally 8
                    }

                    bool isItalic = false;
                    int fontNo = 0;
                    System.Collections.Generic.Stack<string> fontColors = new Stack<string>();
                    foreach (string line in lines)
                    {
                        XmlNode textNode = xml.CreateElement("dcst:Text", "dcst");

                        XmlAttribute vPosition = xml.CreateAttribute("VPosition");
                        vPosition.InnerText = vPos.ToString();
                        textNode.Attributes.Append(vPosition);

                        XmlAttribute vAlign = xml.CreateAttribute("VAlign");
                        if (alignVTop)
                            vAlign.InnerText = "top";
                        else if (alignVCenter)
                            vAlign.InnerText = "center";
                        else
                            vAlign.InnerText = "bottom";
                        textNode.Attributes.Append(vAlign); textNode.Attributes.Append(vAlign);

                        XmlAttribute hAlign = xml.CreateAttribute("HAlign");
                        if (alignLeft)
                            hAlign.InnerText = "left";
                        else if (alignRight)
                            hAlign.InnerText = "right";
                        else
                            hAlign.InnerText = "center";
                        textNode.Attributes.Append(hAlign);

                        XmlAttribute direction = xml.CreateAttribute("Direction");
                        direction.InnerText = "horizontal";
                        textNode.Attributes.Append(direction);

                        int i = 0;
                        var txt = new StringBuilder();
                        var html = new StringBuilder();
                        XmlNode nodeTemp = xml.CreateElement("temp");
                        while (i < line.Length)
                        {
                            if (!isItalic && line.Substring(i).StartsWith("<i>"))
                            {
                                if (txt.Length > 0)
                                {
                                    nodeTemp.InnerText = txt.ToString();
                                    html.Append(nodeTemp.InnerXml);
                                    txt = new StringBuilder();
                                }
                                isItalic = true;
                                i += 2;
                            }
                            else if (isItalic && line.Substring(i).StartsWith("</i>"))
                            {
                                if (txt.Length > 0)
                                {
                                    XmlNode fontNode = xml.CreateElement("dcst:Font", "dcst");

                                    XmlAttribute italic = xml.CreateAttribute("Italic");
                                    italic.InnerText = "yes";
                                    fontNode.Attributes.Append(italic);

                                    fontNode.InnerText = Utilities.RemoveHtmlTags(txt.ToString());
                                    html.Append(fontNode.OuterXml);
                                    txt = new StringBuilder();
                                }
                                isItalic = false;
                                i += 3;
                            }
                            else if (line.Substring(i).StartsWith("<font color=") && line.Substring(i + 3).Contains(">"))
                            {
                                int endOfFont = line.IndexOf(">", i);
                                if (txt.Length > 0)
                                {
                                    nodeTemp.InnerText = txt.ToString();
                                    html.Append(nodeTemp.InnerXml);
                                    txt = new StringBuilder();
                                }
                                string c = line.Substring(i + 12, endOfFont - (i + 12));
                                c = c.Trim('"').Trim('\'').Trim();
                                if (c.StartsWith("#"))
                                    c = c.TrimStart('#').ToUpper().PadLeft(8, 'F');
                                fontColors.Push(c);
                                fontNo++;
                                i += endOfFont - i;
                            }
                            else if (fontNo > 0 && line.Substring(i).StartsWith("</font>"))
                            {
                                if (txt.Length > 0)
                                {
                                    XmlNode fontNode = xml.CreateElement("dcst:Font", "dcst");

                                    XmlAttribute fontColor = xml.CreateAttribute("Color");
                                    fontColor.InnerText = fontColors.Pop();
                                    fontNode.Attributes.Append(fontColor);

                                    fontNode.InnerText = Utilities.RemoveHtmlTags(txt.ToString());
                                    html.Append(fontNode.OuterXml);
                                    txt = new StringBuilder();
                                }
                                fontNo--;
                                i += 6;
                            }
                            else
                            {
                                txt.Append(line.Substring(i, 1));
                            }
                            i++;
                        }
                        if (isItalic)
                        {
                            if (txt.Length > 0)
                            {
                                XmlNode fontNode = xml.CreateElement("dcst:Font", "dcst");

                                XmlAttribute italic = xml.CreateAttribute("Italic");
                                italic.InnerText = "yes";
                                fontNode.Attributes.Append(italic);

                                fontNode.InnerText = Utilities.RemoveHtmlTags(line);
                                html.Append(fontNode.OuterXml);
                            }
                        }
                        else
                        {
                            if (txt.Length > 0)
                            {
                                nodeTemp.InnerText = txt.ToString();
                                html.Append(nodeTemp.InnerXml);
                            }
                        }
                        textNode.InnerXml = html.ToString();

                        subNode.AppendChild(textNode);
                        vPos -= vPosFactor;
                    }

                    mainListFont.AppendChild(subNode);
                    no++;
                }
            }

            var ms = new MemoryStream();
            var writer = new XmlTextWriter(ms, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            xml.Save(writer);
            return Encoding.UTF8.GetString(ms.ToArray()).Trim().Replace("encoding=\"utf-8\"", "encoding=\"UTF-8\"");
        }

        public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        {
            _errorCount = 0;

            var sb = new StringBuilder();
            lines.ForEach(line => sb.AppendLine(line));
            var xml = new XmlDocument();
            xml.LoadXml(sb.ToString().Replace("<dcst:", "<").Replace("</dcst:", "</")); // tags might be prefixed with namespace (or not)... so we just remove them

            var ss = Configuration.Settings.SubtitleSettings;
            try
            {
                ss.InitializeDCinameSettings(true);
                XmlNode node = xml.DocumentElement.SelectSingleNode("Id");
                if (node != null)
                    ss.CurrentDCinemaSubtitleId = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("ReelNumber");
                if (node != null)
                    ss.CurrentDCinemaReelNumber = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("EditRate");
                if (node != null)
                    ss.CurrentDCinemaEditRate = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("Language");
                if (node != null)
                    ss.CurrentDCinemaLanguage = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("ContentTitleText");
                if (node != null)
                    ss.CurrentDCinemaMovieTitle = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("IssueDate");
                if (node != null)
                    ss.CurrentDCinemaIssueDate = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("LoadFont");
                if (node != null)
                    ss.CurrentDCinemaFontUri = node.InnerText;

                node = xml.DocumentElement.SelectSingleNode("SubtitleList/Font");
                if (node != null)
                {                    
                    if (node.Attributes["ID"] != null)
                        ss.CurrentDCinemaFontId = node.Attributes["ID"].InnerText;
                    if (node.Attributes["Size"] != null)
                        ss.CurrentDCinemaFontSize = Convert.ToInt32(node.Attributes["Size"].InnerText);
                    if (node.Attributes["Color"] != null)
                        ss.CurrentDCinemaFontColor = System.Drawing.ColorTranslator.FromHtml("#" + node.Attributes["Color"].InnerText);
                    if (node.Attributes["Effect"] != null)
                        ss.CurrentDCinemaFontEffect = node.Attributes["Effect"].InnerText;
                    if (node.Attributes["EffectColor"] != null)
                        ss.CurrentDCinemaFontEffectColor = System.Drawing.ColorTranslator.FromHtml("#" + node.Attributes["EffectColor"].InnerText);
                }                
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message);
            }

            foreach (XmlNode node in xml.DocumentElement.SelectNodes("//Subtitle"))
            {
                try
                {
                    StringBuilder pText = new StringBuilder();
                    string lastVPosition = string.Empty;
                    foreach (XmlNode innerNode in node.ChildNodes)
                    {
                        switch (innerNode.Name.ToString())
                        {
                            case "Text":
                                if (innerNode.Attributes["VPosition"] != null)
                                {
                                    string vPosition = innerNode.Attributes["VPosition"].InnerText;
                                    if (vPosition != lastVPosition)
                                    {
                                        if (pText.Length > 0 && lastVPosition != string.Empty)
                                            pText.AppendLine();
                                        lastVPosition = vPosition;
                                    }
                                }
                                if (innerNode.Attributes["HAlign"] != null)
                                {
                                    string hAlign = innerNode.Attributes["HAlign"].InnerText;
                                    if (hAlign == "left")
                                    {
                                        if (!pText.ToString().StartsWith("{\\an"))
                                        {
                                            string temp = "{\\an1}" + pText.ToString();
                                            pText = new StringBuilder();
                                            pText.Append(temp);
                                        }
                                    }
                                    else if (hAlign == "right")
                                    {
                                        if (!pText.ToString().StartsWith("{\\an"))
                                        {
                                            string temp = "{\\an3}" + pText.ToString();
                                            pText = new StringBuilder();
                                            pText.Append(temp);
                                        }
                                    }
                                }
                                if (innerNode.ChildNodes.Count == 0)
                                {
                                    pText.Append(innerNode.InnerText);
                                }
                                else
                                {
                                    foreach (XmlNode innerInnerNode in innerNode)
                                    {
                                        if (innerInnerNode.Name == "Font" && innerInnerNode.Attributes["Italic"] != null &&
                                            innerInnerNode.Attributes["Italic"].InnerText.ToLower() == "yes")
                                        {
                                            pText.Append("<i>" + innerInnerNode.InnerText + "</i>");
                                        }
                                        else if (innerInnerNode.Name == "Font" && innerInnerNode.Attributes["Color"] != null)
                                        {
                                            pText.Append("<font color=\"" + GetColorStringFromDCinema(innerInnerNode.Attributes["Color"].Value) + "\">" + innerInnerNode.InnerText + "</font>");
                                        }
                                        else
                                        {
                                            pText.Append(innerInnerNode.InnerText);
                                        }
                                    }
                                }
                                break;
                            default:
                                pText.Append(innerNode.InnerText);
                                break;
                        }
                    }
                    string start = node.Attributes["TimeIn"].InnerText;
                    string end = node.Attributes["TimeOut"].InnerText;

                    subtitle.Paragraphs.Add(new Paragraph(GetTimeCode(start), GetTimeCode(end), pText.ToString()));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    _errorCount++;
                }
            }

            if (subtitle.Paragraphs.Count > 0)
                subtitle.Header = xml.OuterXml; // save id/language/font for later use

            subtitle.Renumber(1);
        }

        private string GetColorStringForDCinema(string p)
        {
            string s = p.ToUpper().Trim();
            if (s.Replace("#", string.Empty).
                Replace("0", string.Empty).
                Replace("1", string.Empty).
                Replace("2", string.Empty).
                Replace("3", string.Empty).
                Replace("4", string.Empty).
                Replace("5", string.Empty).
                Replace("6", string.Empty).
                Replace("7", string.Empty).
                Replace("8", string.Empty).
                Replace("9", string.Empty).
                Replace("A", string.Empty).
                Replace("B", string.Empty).
                Replace("C", string.Empty).
                Replace("D", string.Empty).
                Replace("E", string.Empty).
                Replace("F", string.Empty).Length == 0)
            {
                return s.TrimStart('#');
            }
            else
            {
                return p;
            }
        }

        private string GetColorStringFromDCinema(string p)
        {
            string s = p.ToLower().Trim();
            if (s.Replace("#", string.Empty).
                Replace("0", string.Empty).
                Replace("1", string.Empty).
                Replace("2", string.Empty).
                Replace("3", string.Empty).
                Replace("4", string.Empty).
                Replace("5", string.Empty).
                Replace("6", string.Empty).
                Replace("7", string.Empty).
                Replace("8", string.Empty).
                Replace("9", string.Empty).
                Replace("a", string.Empty).
                Replace("b", string.Empty).
                Replace("c", string.Empty).
                Replace("d", string.Empty).
                Replace("e", string.Empty).
                Replace("f", string.Empty).Length == 0)
            {
                if (s.StartsWith("#"))
                    return s;
                else
                    return "#" + s;
            }
            else
            {
                return p;
            }
        }

        private  TimeCode GetTimeCode(string s)
        {
            string[] parts = s.Split(new char[] { ':', '.', ',' });

            int milliseconds = (int)System.Math.Round(int.Parse(parts[3]) * (1000.0 / frameRate));
            if (milliseconds > 999)
                milliseconds = 999;

            var ts = new TimeSpan(0, int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), milliseconds);
            return new TimeCode(ts);
        }

        private string ConvertToTimeString(TimeCode time)
        {
            int frames = (int)System.Math.Round(time.Milliseconds / (1000.0 / frameRate));
            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", time.Hours, time.Minutes, time.Seconds, frames);
        }

    }
}

