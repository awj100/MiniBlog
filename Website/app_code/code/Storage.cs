using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Xml.Linq;
using System.Xml.XPath;
using HtmlAgilityPack;

public static class Storage
{
    private static string _folder = HostingEnvironment.MapPath("~/posts/");

    public static List<Post> GetAllPosts()
    {
        if (HttpRuntime.Cache["posts"] == null)
            LoadPosts();

        if (HttpRuntime.Cache["posts"] != null)
        {
            return (List<Post>)HttpRuntime.Cache["posts"];
        }
        return new List<Post>();
    }

    // Can this be done async?
    public static void Save(Post post)
    {
        string file = Path.Combine(_folder, post.ID + ".xml");
        post.LastModified = DateTime.UtcNow;

        post.Content = CleanContent(post.Content);

        XDocument doc = new XDocument(
            new XElement("post",
                         new XElement("title", post.Title),
                         new XElement("slug", post.Slug),
                         new XElement("author", post.Author),
                         new XElement("pubDate", post.PubDate.ToString("yyyy-MM-dd HH:mm:ss")),
                         new XElement("lastModified", post.LastModified.ToString("yyyy-MM-dd HH:mm:ss")),
                         new XElement("excerpt", post.Excerpt),
                         new XElement("content", post.Content),
                         new XElement("ispublished", post.IsPublished),
                         new XElement("categories", string.Empty),
                         new XElement("comments", string.Empty)
                ));

        XElement categories = doc.XPathSelectElement("post/categories");
        foreach (string category in post.Categories)
        {
            categories.Add(new XElement("category", category));
        }

        XElement comments = doc.XPathSelectElement("post/comments");
        foreach (Comment comment in post.Comments)
        {
            comments.Add(
                         new XElement("comment",
                                      new XElement("author", comment.Author),
                                      new XElement("email", comment.Email),
                                      new XElement("website", comment.Website),
                                      new XElement("ip", comment.Ip),
                                      new XElement("userAgent", comment.UserAgent),
                                      new XElement("date", comment.PubDate.ToString("yyyy-MM-dd HH:m:ss")),
                                      new XElement("content", comment.Content),
                                      new XAttribute("isAdmin", comment.IsAdmin),
                                      new XAttribute("isApproved", comment.IsApproved),
                                      new XAttribute("id", comment.ID)
                             ));
        }

        if (!File.Exists(file)) // New post
        {
            var posts = GetAllPosts();
            posts.Insert(0, post);
            posts.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
            HttpRuntime.Cache.Insert("posts", posts);
        }
        else
        {
            Blog.ClearStartPageCache();
        }

        doc.Save(file);
    }

    public static void Delete(Post post)
    {
        var posts = GetAllPosts();
        string file = Path.Combine(_folder, post.ID + ".xml");
        File.Delete(file);
        posts.Remove(post);
        Blog.ClearStartPageCache();
    }

    private static void LoadPosts()
    {
        if (!Directory.Exists(_folder))
            Directory.CreateDirectory(_folder);

        List<Post> list = new List<Post>();

        // Can this be done in parallel to speed it up?
        foreach (string file in Directory.EnumerateFiles(_folder, "*.xml", SearchOption.TopDirectoryOnly))
        {
            XElement doc = XElement.Load(file);

            Post post = new Post
                            {
                                ID = Path.GetFileNameWithoutExtension(file),
                                Title = ReadValue(doc, "title"),
                                Author = ReadValue(doc, "author"),
                                Excerpt = ReadValue(doc, "excerpt"),
                                Content = ReadValue(doc, "content"),
                                Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                                PubDate = DateTime.Parse(ReadValue(doc, "pubDate")),
                                LastModified = DateTime.Parse(ReadValue(doc, "lastModified", DateTime.Now.ToString())),
                                IsPublished = bool.Parse(ReadValue(doc, "ispublished", "true")),
                            };

            LoadCategories(post, doc);
            LoadComments(post, doc);
            list.Add(post);
        }

        if (list.Count > 0)
        {
            list.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
            HttpRuntime.Cache.Insert("posts", list);
        }
    }

    private static void LoadCategories(Post post, XElement doc)
    {
        XElement categories = doc.Element("categories");
        if (categories == null)
            return;

        List<string> list = new List<string>();

        foreach (var node in categories.Elements("category"))
        {
            list.Add(node.Value);
        }

        post.Categories = list.ToArray();
    }
    private static void LoadComments(Post post, XElement doc)
    {
        var comments = doc.Element("comments");

        if (comments == null)
            return;

        foreach (var node in comments.Elements("comment"))
        {
            Comment comment = new Comment()
            {
                ID = ReadAttribute(node, "id"),
                Author = ReadValue(node, "author"),
                Email = ReadValue(node, "email"),
                Website = ReadValue(node, "website"),
                Ip = ReadValue(node, "ip"),
                UserAgent = ReadValue(node, "userAgent"),
                IsAdmin = bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                IsApproved = bool.Parse(ReadAttribute(node, "isApproved", "true")),
                Content = ReadValue(node, "content").Replace("\n", "<br />"),
                PubDate = DateTime.Parse(ReadValue(node, "date", "2000-01-01")),
            };

            post.Comments.Add(comment);
        }
    }

    private static string ReadValue(XElement doc, XName name, string defaultValue = "")
    {
        if (doc.Element(name) != null)
            return doc.Element(name).Value;

        return defaultValue;
    }

    private static string ReadAttribute(XElement element, XName name, string defaultValue = "")
    {
        if (element.Attribute(name) != null)
            return element.Attribute(name).Value;

        return defaultValue;
    }

    private static string EnsureSafeHtmlEntities(string content)
    {
        // http://www.w3.org/TR/html4/sgml/entities.html
        var entities = new[]
                           {
                               new Tuple<string, string>("&nbsp;", "&#160;"),
                               new Tuple<string, string>("&iexcl;", "&#161;"),
                               new Tuple<string, string>("&cent;", "&#162;"),
                               new Tuple<string, string>("&pound;", "&#163;"),
                               new Tuple<string, string>("&curren;", "&#164;"),
                               new Tuple<string, string>("&yen;", "&#165;"),
                               new Tuple<string, string>("&brvbar;", "&#166;"),
                               new Tuple<string, string>("&sect;", "&#167;"),
                               new Tuple<string, string>("&uml;", "&#168;"),
                               new Tuple<string, string>("&copy;", "&#169;"),
                               new Tuple<string, string>("&ordf;", "&#170;"),
                               new Tuple<string, string>("&laquo;", "&#171;"),
                               new Tuple<string, string>("&not;", "&#172;"),
                               new Tuple<string, string>("&shy;", "&#173;"),
                               new Tuple<string, string>("&reg;", "&#174;"),
                               new Tuple<string, string>("&macr;", "&#175;"),
                               new Tuple<string, string>("&deg;", "&#176;"),
                               new Tuple<string, string>("&plusmn;", "&#177;"),
                               new Tuple<string, string>("&sup2;", "&#178;"),
                               new Tuple<string, string>("&sup3;", "&#179;"),
                               new Tuple<string, string>("&acute;", "&#180;"),
                               new Tuple<string, string>("&micro;", "&#181;"),
                               new Tuple<string, string>("&para;", "&#182;"),
                               new Tuple<string, string>("&middot;", "&#183;"),
                               new Tuple<string, string>("&cedil;", "&#184;"),
                               new Tuple<string, string>("&sup1;", "&#185;"),
                               new Tuple<string, string>("&ordm;", "&#186;"),
                               new Tuple<string, string>("&raquo;", "&#187;"),
                               new Tuple<string, string>("&frac14;", "&#188;"),
                               new Tuple<string, string>("&frac12;", "&#189;"),
                               new Tuple<string, string>("&frac34;", "&#190;"),
                               new Tuple<string, string>("&iquest;", "&#191;"),
                               new Tuple<string, string>("&Agrave;", "&#192;"),
                               new Tuple<string, string>("&Aacute;", "&#193;"),
                               new Tuple<string, string>("&Acirc;", "&#194;"),
                               new Tuple<string, string>("&Atilde;", "&#195;"),
                               new Tuple<string, string>("&Auml;", "&#196;"),
                               new Tuple<string, string>("&Aring;", "&#197;"),
                               new Tuple<string, string>("&AElig;", "&#198;"),
                               new Tuple<string, string>("&Ccedil;", "&#199;"),
                               new Tuple<string, string>("&Egrave;", "&#200;"),
                               new Tuple<string, string>("&Eacute;", "&#201;"),
                               new Tuple<string, string>("&Ecirc;", "&#202;"),
                               new Tuple<string, string>("&Euml;", "&#203;"),
                               new Tuple<string, string>("&Igrave;", "&#204;"),
                               new Tuple<string, string>("&Iacute;", "&#205;"),
                               new Tuple<string, string>("&Icirc;", "&#206;"),
                               new Tuple<string, string>("&Iuml;", "&#207;"),
                               new Tuple<string, string>("&ETH;", "&#208;"),
                               new Tuple<string, string>("&Ntilde;", "&#209;"),
                               new Tuple<string, string>("&Ograve;", "&#210;"),
                               new Tuple<string, string>("&Oacute;", "&#211;"),
                               new Tuple<string, string>("&Ocirc;", "&#212;"),
                               new Tuple<string, string>("&Otilde;", "&#213;"),
                               new Tuple<string, string>("&Ouml;", "&#214;"),
                               new Tuple<string, string>("&times;", "&#215;"),
                               new Tuple<string, string>("&Oslash;", "&#216;"),
                               new Tuple<string, string>("&Ugrave;", "&#217;"),
                               new Tuple<string, string>("&Uacute;", "&#218;"),
                               new Tuple<string, string>("&Ucirc;", "&#219;"),
                               new Tuple<string, string>("&Uuml;", "&#220;"),
                               new Tuple<string, string>("&Yacute;", "&#221;"),
                               new Tuple<string, string>("&THORN;", "&#222;"),
                               new Tuple<string, string>("&szlig;", "&#223;"),
                               new Tuple<string, string>("&agrave;", "&#224;"),
                               new Tuple<string, string>("&aacute;", "&#225;"),
                               new Tuple<string, string>("&acirc;", "&#226;"),
                               new Tuple<string, string>("&atilde;", "&#227;"),
                               new Tuple<string, string>("&auml;", "&#228;"),
                               new Tuple<string, string>("&aring;", "&#229;"),
                               new Tuple<string, string>("&aelig;", "&#230;"),
                               new Tuple<string, string>("&ccedil;", "&#231;"),
                               new Tuple<string, string>("&egrave;", "&#232;"),
                               new Tuple<string, string>("&eacute;", "&#233;"),
                               new Tuple<string, string>("&ecirc;", "&#234;"),
                               new Tuple<string, string>("&euml;", "&#235;"),
                               new Tuple<string, string>("&igrave;", "&#236;"),
                               new Tuple<string, string>("&iacute;", "&#237;"),
                               new Tuple<string, string>("&icirc;", "&#238;"),
                               new Tuple<string, string>("&iuml;", "&#239;"),
                               new Tuple<string, string>("&eth;", "&#240;"),
                               new Tuple<string, string>("&ntilde;", "&#241;"),
                               new Tuple<string, string>("&ograve;", "&#242;"),
                               new Tuple<string, string>("&oacute;", "&#243;"),
                               new Tuple<string, string>("&ocirc;", "&#244;"),
                               new Tuple<string, string>("&otilde;", "&#245;"),
                               new Tuple<string, string>("&ouml;", "&#246;"),
                               new Tuple<string, string>("&divide;", "&#247;"),
                               new Tuple<string, string>("&oslash;", "&#248;"),
                               new Tuple<string, string>("&ugrave;", "&#249;"),
                               new Tuple<string, string>("&uacute;", "&#250;"),
                               new Tuple<string, string>("&ucirc;", "&#251;"),
                               new Tuple<string, string>("&uuml;", "&#252;"),
                               new Tuple<string, string>("&yacute;", "&#253;"),
                               new Tuple<string, string>("&thorn;", "&#254;"),
                               new Tuple<string, string>("&yuml;", "&#255;"),
                               new Tuple<string, string>("&fnof;", "&#402;"),
                               new Tuple<string, string>("&Alpha;", "&#913;"),
                               new Tuple<string, string>("&Beta;", "&#914;"),
                               new Tuple<string, string>("&Gamma;", "&#915;"),
                               new Tuple<string, string>("&Delta;", "&#916;"),
                               new Tuple<string, string>("&Epsilon;", "&#917;"),
                               new Tuple<string, string>("&Zeta;", "&#918;"),
                               new Tuple<string, string>("&Eta;", "&#919;"),
                               new Tuple<string, string>("&Theta;", "&#920;"),
                               new Tuple<string, string>("&Iota;", "&#921;"),
                               new Tuple<string, string>("&Kappa;", "&#922;"),
                               new Tuple<string, string>("&Lambda;", "&#923;"),
                               new Tuple<string, string>("&Mu;", "&#924;"),
                               new Tuple<string, string>("&Nu;", "&#925;"),
                               new Tuple<string, string>("&Xi;", "&#926;"),
                               new Tuple<string, string>("&Omicron;", "&#927;"),
                               new Tuple<string, string>("&Pi;", "&#928;"),
                               new Tuple<string, string>("&Rho;", "&#929;"),
                               new Tuple<string, string>("&Sigma;", "&#931;"),
                               new Tuple<string, string>("&Tau;", "&#932;"),
                               new Tuple<string, string>("&Upsilon;", "&#933;"),
                               new Tuple<string, string>("&Phi;", "&#934;"),
                               new Tuple<string, string>("&Chi;", "&#935;"),
                               new Tuple<string, string>("&Psi;", "&#936;"),
                               new Tuple<string, string>("&Omega;", "&#937;"),
                               new Tuple<string, string>("&alpha;", "&#945;"),
                               new Tuple<string, string>("&beta;", "&#946;"),
                               new Tuple<string, string>("&gamma;", "&#947;"),
                               new Tuple<string, string>("&delta;", "&#948;"),
                               new Tuple<string, string>("&epsilon;", "&#949;"),
                               new Tuple<string, string>("&zeta;", "&#950;"),
                               new Tuple<string, string>("&eta;", "&#951;"),
                               new Tuple<string, string>("&theta;", "&#952;"),
                               new Tuple<string, string>("&iota;", "&#953;"),
                               new Tuple<string, string>("&kappa;", "&#954;"),
                               new Tuple<string, string>("&lambda;", "&#955;"),
                               new Tuple<string, string>("&mu;", "&#956;"),
                               new Tuple<string, string>("&nu;", "&#957;"),
                               new Tuple<string, string>("&xi;", "&#958;"),
                               new Tuple<string, string>("&omicron;", "&#959;"),
                               new Tuple<string, string>("&pi;", "&#960;"),
                               new Tuple<string, string>("&rho;", "&#961;"),
                               new Tuple<string, string>("&sigmaf;", "&#962;"),
                               new Tuple<string, string>("&sigma;", "&#963;"),
                               new Tuple<string, string>("&tau;", "&#964;"),
                               new Tuple<string, string>("&upsilon;", "&#965;"),
                               new Tuple<string, string>("&phi;", "&#966;"),
                               new Tuple<string, string>("&chi;", "&#967;"),
                               new Tuple<string, string>("&psi;", "&#968;"),
                               new Tuple<string, string>("&omega;", "&#969;"),
                               new Tuple<string, string>("&thetasym;", "&#977;"),
                               new Tuple<string, string>("&upsih;", "&#978;"),
                               new Tuple<string, string>("&piv;", "&#982;"),
                               new Tuple<string, string>("&bull;", "&#8226;"),
                               new Tuple<string, string>("&hellip;", "&#8230;"),
                               new Tuple<string, string>("&prime;", "&#8242;"),
                               new Tuple<string, string>("&Prime;", "&#8243;"),
                               new Tuple<string, string>("&oline;", "&#8254;"),
                               new Tuple<string, string>("&frasl;", "&#8260;"),
                               new Tuple<string, string>("&weierp;", "&#8472;"),
                               new Tuple<string, string>("&image;", "&#8465;"),
                               new Tuple<string, string>("&real;", "&#8476;"),
                               new Tuple<string, string>("&trade;", "&#8482;"),
                               new Tuple<string, string>("&alefsym;", "&#8501;"),
                               new Tuple<string, string>("&larr;", "&#8592;"),
                               new Tuple<string, string>("&uarr;", "&#8593;"),
                               new Tuple<string, string>("&rarr;", "&#8594;"),
                               new Tuple<string, string>("&darr;", "&#8595;"),
                               new Tuple<string, string>("&harr;", "&#8596;"),
                               new Tuple<string, string>("&crarr;", "&#8629;"),
                               new Tuple<string, string>("&lArr;", "&#8656;"),
                               new Tuple<string, string>("&uArr;", "&#8657;"),
                               new Tuple<string, string>("&rArr;", "&#8658;"),
                               new Tuple<string, string>("&dArr;", "&#8659;"),
                               new Tuple<string, string>("&hArr;", "&#8660;"),
                               new Tuple<string, string>("&forall;", "&#8704;"),
                               new Tuple<string, string>("&part;", "&#8706;"),
                               new Tuple<string, string>("&exist;", "&#8707;"),
                               new Tuple<string, string>("&empty;", "&#8709;"),
                               new Tuple<string, string>("&nabla;", "&#8711;"),
                               new Tuple<string, string>("&isin;", "&#8712;"),
                               new Tuple<string, string>("&notin;", "&#8713;"),
                               new Tuple<string, string>("&ni;", "&#8715;"),
                               new Tuple<string, string>("&prod;", "&#8719;"),
                               new Tuple<string, string>("&sum;", "&#8721;"),
                               new Tuple<string, string>("&minus;", "&#8722;"),
                               new Tuple<string, string>("&lowast;", "&#8727;"),
                               new Tuple<string, string>("&radic;", "&#8730;"),
                               new Tuple<string, string>("&prop;", "&#8733;"),
                               new Tuple<string, string>("&infin;", "&#8734;"),
                               new Tuple<string, string>("&ang;", "&#8736;"),
                               new Tuple<string, string>("&and;", "&#8743;"),
                               new Tuple<string, string>("&or;", "&#8744;"),
                               new Tuple<string, string>("&cap;", "&#8745;"),
                               new Tuple<string, string>("&cup;", "&#8746;"),
                               new Tuple<string, string>("&int;", "&#8747;"),
                               new Tuple<string, string>("&there4;", "&#8756;"),
                               new Tuple<string, string>("&sim;", "&#8764;"),
                               new Tuple<string, string>("&cong;", "&#8773;"),
                               new Tuple<string, string>("&asymp;", "&#8776;"),
                               new Tuple<string, string>("&ne;", "&#8800;"),
                               new Tuple<string, string>("&equiv;", "&#8801;"),
                               new Tuple<string, string>("&le;", "&#8804;"),
                               new Tuple<string, string>("&ge;", "&#8805;"),
                               new Tuple<string, string>("&sub;", "&#8834;"),
                               new Tuple<string, string>("&sup;", "&#8835;"),
                               new Tuple<string, string>("&nsub;", "&#8836;"),
                               new Tuple<string, string>("&sube;", "&#8838;"),
                               new Tuple<string, string>("&supe;", "&#8839;"),
                               new Tuple<string, string>("&oplus;", "&#8853;"),
                               new Tuple<string, string>("&otimes;", "&#8855;"),
                               new Tuple<string, string>("&perp;", "&#8869;"),
                               new Tuple<string, string>("&sdot;", "&#8901;"),
                               new Tuple<string, string>("&lceil;", "&#8968;"),
                               new Tuple<string, string>("&rceil;", "&#8969;"),
                               new Tuple<string, string>("&lfloor;", "&#8970;"),
                               new Tuple<string, string>("&rfloor;", "&#8971;"),
                               new Tuple<string, string>("&lang;", "&#9001;"),
                               new Tuple<string, string>("&rang;", "&#9002;"),
                               new Tuple<string, string>("&loz;", "&#9674;"),
                               new Tuple<string, string>("&spades;", "&#9824;"),
                               new Tuple<string, string>("&clubs;", "&#9827;"),
                               new Tuple<string, string>("&hearts;", "&#9829;"),
                               new Tuple<string, string>("&diams;", "&#9830;"),
                               new Tuple<string, string>("&quot;", "&#34;"),
                               new Tuple<string, string>("&amp;", "&#38;"),
                               new Tuple<string, string>("&lt;", "&#60;"),
                               new Tuple<string, string>("&gt;", "&#62;"),
                               new Tuple<string, string>("&OElig;", "&#338;"),
                               new Tuple<string, string>("&oelig;", "&#339;"),
                               new Tuple<string, string>("&Scaron;", "&#352;"),
                               new Tuple<string, string>("&scaron;", "&#353;"),
                               new Tuple<string, string>("&Yuml;", "&#376;"),
                               new Tuple<string, string>("&circ;", "&#710;"),
                               new Tuple<string, string>("&tilde;", "&#732;"),
                               new Tuple<string, string>("&ensp;", "&#8194;"),
                               new Tuple<string, string>("&emsp;", "&#8195;"),
                               new Tuple<string, string>("&thinsp;", "&#8201;"),
                               new Tuple<string, string>("&zwnj;", "&#8204;"),
                               new Tuple<string, string>("&zwj;", "&#8205;"),
                               new Tuple<string, string>("&lrm;", "&#8206;"),
                               new Tuple<string, string>("&rlm;", "&#8207;"),
                               new Tuple<string, string>("&ndash;", "&#8211;"),
                               new Tuple<string, string>("&mdash;", "&#8212;"),
                               new Tuple<string, string>("&lsquo;", "&#8216;"),
                               new Tuple<string, string>("&rsquo;", "&#8217;"),
                               new Tuple<string, string>("&sbquo;", "&#8218;"),
                               new Tuple<string, string>("&ldquo;", "&#8220;"),
                               new Tuple<string, string>("&rdquo;", "&#8221;"),
                               new Tuple<string, string>("&bdquo;", "&#8222;"),
                               new Tuple<string, string>("&dagger;", "&#8224;"),
                               new Tuple<string, string>("&Dagger;", "&#8225;"),
                               new Tuple<string, string>("&permil;", "&#8240;"),
                               new Tuple<string, string>("&lsaquo;", "&#8249;"),
                               new Tuple<string, string>("&rsaquo;", "&#8250;"),
                               new Tuple<string, string>("&euro;", "&#8364;")
                           };

        var sbContent = new StringBuilder(content);

        return entities.Aggregate(sbContent, (current, entity) => current.Replace(entity.Item1, entity.Item2)).ToString();
    }
    
    private static string CleanContent(string content)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        RemoveXmlnsAttrs(doc.DocumentNode);

        return EnsureSafeHtmlEntities(doc.DocumentNode.InnerHtml);
    }

    private static void RemoveXmlnsAttrs(HtmlNode node)
    {
        node.Attributes.Remove("xmlns");

        foreach (var nextNode in node.ChildNodes)
        {
            RemoveXmlnsAttrs(nextNode);
        }
    }
}