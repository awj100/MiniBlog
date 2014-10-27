(function ($) {

    // #region Helpers

    function ConvertMarkupToValidXhtml(markup) {
        var docImplementation = document.implementation;
        var htmlDocument = docImplementation.createHTMLDocument("temp");
        var xHtmlDocument = docImplementation.createDocument('http://www.w3.org/1999/xhtml', 'html', null);
        var xhtmlBody = xHtmlDocument.createElementNS('http://www.w3.org/1999/xhtml', 'body');

        htmlDocument.body.innerHTML = "<div id=\"mbID\">" + markup + "</div>";

        xHtmlDocument.documentElement.appendChild(xhtmlBody);
        xHtmlDocument.importNode(htmlDocument.body, true);
        xhtmlBody.appendChild(htmlDocument.body.firstChild);

        /<body.*?><div>(.*?)<\/div><\/body>/im.exec(xHtmlDocument.getElementById("mbID").innerHTML);
        return RegExp.$1;
    }

    // #endregion

    var postId, isNew,
        txtTitle, txtExcerpt, txtContent, txtMessage, txtImage, chkPublish,
        btnNew, btnEdit, btnDelete, btnSave, btnCancel, blogPath,

    editPost = function () {
        txtTitle.attr('contentEditable', true);
        txtExcerpt.attr('contentEditable', true);
		txtExcerpt.css({ minHeight: "100px" });
        txtExcerpt.parent().css('display', 'block');
        txtContent.wysiwyg({ hotKeys: {}, activeToolbarClass: "active" });
        txtContent.css({ minHeight: "400px" });
        txtContent.focus();

        btnNew.attr("disabled", true);
        btnEdit.attr("disabled", true);
        btnSave.removeAttr("disabled");
        btnCancel.removeAttr("disabled");
        chkPublish.removeAttr("disabled");

        showCategoriesForEditing();

        toggleSourceView();

        $("#tools").fadeIn().css("display", "inline-block");
    },
    cancelEdit = function () {
        if (isNew) {
            if (confirm("Do you want to leave this page?")) {
                history.back();
            }
        } else {
            txtTitle.removeAttr('contentEditable');
            txtExcerpt.removeAttr('contentEditable');
            txtExcerpt.parent().css('display', 'none');
            txtContent.removeAttr('contentEditable');
            btnCancel.focus();

            btnNew.removeAttr("disabled");
            btnEdit.removeAttr("disabled");
            btnSave.attr("disabled", true);
            btnCancel.attr("disabled", true);
            chkPublish.attr("disabled", true);

            showCategoriesForDisplay();

            $("#tools").fadeOut();
        }
    },
    toggleSourceView = function () {
        $(".source").bind("click", function () {
            var self = $(this);
            if (self.attr("data-cmd") === "source") {
                self.attr("data-cmd", "design");
                self.addClass("active");
                txtContent.text(txtContent.html());
            } else {
                self.attr("data-cmd", "source");
                self.removeClass("active");
                txtContent.html(txtContent.text());
            }
        });
    },
    savePost = function (e) {
        if ($(".source").attr("data-cmd") === "design") {
            $(".source").click();
        }

        txtContent.cleanHtml();

        var parsedDOM;

        /*  IE9 doesn't support text/html MimeType https://github.com/madskristensen/MiniBlog/issues/35
        
            parsedDOM = new DOMParser().parseFromString(txtContent.html(), 'text/html');
            parsedDOM = new XMLSerializer().serializeToString(parsedDOM);

            /<body>(.*)<\/body>/im.exec(parsedDOM);
            parsedDOM = RegExp.$1;
        
        */

        /* When its time to drop IE9 support toggle commented region with 
           the following statement and ConvertMarkupToXhtml function */
        //parsedDOM = ConvertMarkupToValidXhtml('<p style="" class="ecxMsoNormal"><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">You might be looking at that title and thinking <em><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">the Euro symbol doesn’t fit for an ‘S’ like the dollar symbol does</span></em>. And that perfectly matches the topic of this blog post.<br> A <a target="_blank" href="https://news.layervault.com/stories/29331-results-dn-survey-of-design-salaries-2014"> recent survey</a> polled the salaries of designers around the world (it appears that these are designers with a web leaning). Much is made the fact that design salaries in the US are greater than Europe, and <a target="_blank" href="http://www.smashingmagazine.com/smashing-newsletter-issue-115/#a6">Smashing Magazine</a> even concluded that it "might be time to consider leaving Europe for a better perspective". But I think the poll is fundamentally flawed.<br>Incidentally, there`s a visual representation of the results <a target="_blank" href="http://ivanamcconnell.com/design-survey.html">here</a>.</span></p><h3><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">Living standards</span></h3><p class="ecxMsoNormal"><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">Differentliving standards occur in each country around the world &ndash; even between cities in the same country (for example, compare London with Glasgow); different living standards incur differentcosts, which are then reflected in local salaries.<br>Which gives us problem 1: this survey doesn`t take into account the local costs of living, which always has a direct effect on salaries.</span></p><h3><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">Consumer power</span></h3><p class="ecxMsoNormal"><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">Forexample, the poll collects salaries across Europe and unfairly comparesthem to the USA. Switzerland and Scandinavia have high living costs &ndash; the highest in the world according to<a target="_blank" href="http://www.numbeo.com/cost-of-living/rankings_by_country.jsp">this website</a>. And yet cross the border from Switzerland into neighbouring Italy and the consumer price index drops by a third. In theory, one should expect that salaries in Italy be proportionally lowerthan those in Switzerland. To lump these two countriestogether and compare them to a single country on the other side of the world is almost meaningless.<br>A similar drop is seen between Finland and neighbouring Russia.<br>With that in mind, let’s take a look at the weighing of votes.<br>Norway (Consumer Price Index: 145.16), Sweden (98.53), Denmark (115.09) and Switzerland (142.49) have a total of 19 votes; Turkey (8 votes, CPI:53.64)), Russia (6 votes, 61.75), Italy (5 votes, 95.76), Spain (4 votes, 75.68) and Portugal (3 votes, 67.78) whichall exhibit a sizeable difference in their consumer price index and therefore lower salaries should be anticipated, outweigh the richer countries with 23 votes.<br>And then there are Eastern European countries with small votes: Hungary (CPI: 57), Czech Republic (56.49), Poland (53.36), Romania (49.2), Belarus (50.34), Ukraine (44.33), Latvia (64.1), Moldova (38.21), Bulgaria (49.65) and Lithuania (59.68), all loweringthe average European salary. Nothing wrong with them voting, it just skews the statistics.<br>So comparing USA and European salaries is akin to comparing, say, Swiss salaries with those of all countries in the Americas - how would the USA(with a Consumer Price Index of 76.97) like to have their salaries aggregated with those from Honduras (CPI: 52.67),Guatemala (51.51), Colombia (50.14) and Bolivia (42.33)?</span></p><h3><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">Conclusion</span></h3><p class="ecxMsoNormal"><span style="font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;;">These figures are all but meaningless without some form of&nbsp;<a target="_blank" href="http://en.wikipedia.org/wiki/Normalization_%28statistics%29">statistical normalisation</a>.<br>As Einstein taught us, all things are relative. And without any relativity, statistics can be used to prove anything &ndash;<a target="_blank" href="https://www.youtube.com/watch?v=CpmDIP3Fn2Y">40% of people know that</a>.</span></p>');
        parsedDOM = ConvertMarkupToValidXhtml(txtContent.html());

        $.post(blogPath + "/post.ashx?mode=save", {
            id: postId,
            isPublished: chkPublish[0].checked,
            title: txtTitle.text().trim(),
            excerpt: txtExcerpt.text().trim(),
            content: parsedDOM,
            categories: getPostCategories(),
            __RequestVerificationToken: document.querySelector("input[name=__RequestVerificationToken]").getAttribute("value")
        })
          .success(function (data) {
              if (isNew) {
                  location.href = data;
                  return;
              }

              showMessage(true, "The post was saved successfully");
              cancelEdit(e);
          })
          .fail(function (data) {
              if (data.status === 409) {
                  showMessage(false, "The title is already in use");
              } else {
                  showMessage(false, "Something bad happened. Server reported " + data.status + " " + data.statusText);
              }
          });
    },
    deletePost = function () {
        if (confirm("Are you sure you want to delete this post?")) {
            $.post(blogPath + "/post.ashx?mode=delete", { id: postId, __RequestVerificationToken: document.querySelector("input[name=__RequestVerificationToken]").getAttribute("value") })
                .success(function () { location.href = blogPath+"/"; })
                .fail(function () { showMessage(false, "Something went wrong. Please try again"); });
        }
    },
    showMessage = function (success, message) {
        var className = success ? "alert-success" : "alert-error";
        txtMessage.addClass(className);
        txtMessage.text(message);
        txtMessage.parent().fadeIn();

        setTimeout(function () {
            txtMessage.parent().fadeOut("slow", function () {
                txtMessage.removeClass(className);
            });
        }, 4000);
    },
    getPostCategories = function () {
        var categories = '';

        if ($("#txtCategories").length > 0) {
            categories = $("#txtCategories").val();
        } else {
            $("ul.categories li a").each(function (index, item) {
                if (categories.length > 0) {
                    categories += ",";
                }
                categories += $(item).html();
            });
        }
        return categories;
    },
    showCategoriesForEditing = function () {
        var firstItemPassed = false;
        var categoriesString = getPostCategories();
        $("ul.categories li").each(function (index, item) {
            if (!firstItemPassed) {
                firstItemPassed = true;
            } else {
                $(item).remove();
            }
        });
        $("ul.categories").append("<li><input id='txtCategories' class='form-control' /></li>");
        $("#txtCategories").val(categoriesString);
    },
    showCategoriesForDisplay = function () {
        if ($("#txtCategories").length > 0) {
            var categoriesArray = $("#txtCategories").val().split(',');
            $("#txtCategories").parent().remove();

            $.each(categoriesArray, function (index, category) {
                $("ul.categories").append(' <li itemprop="articleSection" title="' + category + '"> <a href="'+blogPath+'/category/' + encodeURIComponent(category.toLowerCase()) + '">' + category + '</a> </li> ');
            });
        }
    };

    postId = $("[itemprop~='blogPost']").attr("data-id");

    txtTitle = $("[itemprop~='blogPost'] [itemprop~='name']");
    txtExcerpt = $("[itemprop~='description']");
    txtContent = $("[itemprop~='articleBody']");
    txtMessage = $("#admin .alert");
    txtImage = $("#admin #txtImage");

    btnNew = $("#btnNew");
    btnEdit = $("#btnEdit").bind("click", editPost);
    btnDelete = $("#btnDelete").bind("click", deletePost);
    btnSave = $("#btnSave").bind("click", savePost);
    btnCancel = $("#btnCancel").bind("click", cancelEdit);
    chkPublish = $("#ispublished").find("input[type=checkbox]");
    blogPath = $("#admin").data("blogPath");

    isNew = location.pathname.replace(/\//g, "") === blogPath.replace(/\//g, "") + "postnew";

    $(document).keyup(function (e) {
        if (!document.activeElement.isContentEditable) {
            if (e.keyCode === 46) { // Delete key
                deletePost();
            } else if (e.keyCode === 27) { // ESC key
                cancelEdit();
            }
        }
    });

    $('.uploadimage').click(function (e) {
        e.preventDefault();
        $('#txtImage').click();
    });

    if (isNew) {
        editPost();
        $("#ispublished").fadeIn();
        chkPublish[0].checked = true;
    } else if (txtTitle !== null && txtTitle.length === 1 && location.pathname.length > 1) {
        btnEdit.removeAttr("disabled");
        btnDelete.removeAttr("disabled");
        $("#ispublished").css({ "display": "inline" });
    }
})(jQuery);
