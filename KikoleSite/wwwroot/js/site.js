/* positions autovalidation */
$(function () {
    $("#positionSubmission").on("change", function (e) {
        if ($("#positionSubmission").val() != "0") {
            $("#submitPosition").click();
        }
    });
});

/* years autocompletion */
$(function () {
    var availableTags = [];
    for (var i = 1850; i <= 2010; i++)
        availableTags.push(i.toString());
    $("#birthYearValue").autocomplete({
        source: availableTags,
        select: function (e, i) {
            $("#birthYearValue").val(i.item.value);
            if ($("#submitYear").length > 0) {
                $("#submitYear").click();
            }
        },
        minLength: 1
    });
});

/* countries autocompletion */
$(function () {
    $("#countryName").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/Home/AutoCompleteCountries/',
                data: {
                    "prefix": request.term
                },
                type: "POST",
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.Value,
                            value: item.Key
                        };
                    }))
                }
            });
        },
        select: function (e, i) {
            $("#countryId").val(i.item.value);
            if ($("#submitCountry").length > 0) {
                $("#submitCountry").click();
            }
        },
        minLength: 1
    });
});

/* clubs autocompletion */
var autocompleteClubs = function (clubIdName, submit) {
    $(clubIdName).autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/Home/AutoCompleteClubs/',
                data: {
                    "prefix": request.term
                },
                type: "POST",
                success: function (data) {
                    response($.map(data, function (item) {
                        return item;
                    }))
                }
            });
        },
        select: function (e, i) {
            $(clubIdName).val(i.item.value);
            if (submit && $("#submitClub").length > 0) {
                $("#submitClub").click();
            }
        },
        minLength: 1
    });
};
$(function() {
    autocompleteClubs("#clubName", true);
    for (let i = 0; i < 15; i++) {
        autocompleteClubs("#Club" + i, false);
    }
});

/* logins autocompletion */
var autocompleteLogins = function (logins, fieldId) {
    $(fieldId).autocomplete({
        source: logins,
        select: function (e, i) {
            $(fieldId).val(i.item.value);
        },
        minLength: 1
    });
};

var coll = document.getElementsByClassName("collapsible");
for (var i = 0; i < coll.length; i++) {
    coll[i].addEventListener("click", function () {
        this.classList.toggle("active");
        var content = this.nextElementSibling;
        if (content.style.display === "block") {
            content.style.display = "none";
        } else {
            content.style.display = "block";
        }
    });
}

function drawChart() {
    // Set Data
    var activityDatas = [['Week', 'Players']];
    $.ajax({
        url: '/Leaderboard/GetActivityDatas/',
        data: {},
        type: "GET",
        async: false,
        success: function (data) {
            data.forEach(item => activityDatas.push([item.Key, item.Value]));
        }
    });
    var data = google.visualization.arrayToDataTable(activityDatas);
    // Set Options
    var options = {
        hAxis: { title: 'Week number' },
        vAxis: { title: 'Active players' },
        legend: 'none',
        width: 900,
        height: 506
    };
    // Draw Chart
    var chart = new google.visualization.LineChart(document.getElementById('activityChart'));
    chart.draw(data, options);
}