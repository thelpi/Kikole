$(document).ready(function () {
    /* loading google graph lib */
    if (document.getElementById('googleChartEnabler')) {
        google.charts.load('current', { packages: ['corechart'] });
        google.charts.setOnLoadCallback(drawStatisticPageCharts);
    }
});

/* leaderboard loading */
var loadGlobalLeaderboard = function (sortType, dateMin, dateMax, noUserInTableText) {
    $.ajax({
        url: '/leaderboard-details?sortType=' + sortType + '&minimalDate=' + dateMin + '&maximalDate=' + dateMax,
        type: "GET",
        dataType: "json",
        beforeSend: function () {
            $("#loading-image").show();
            $("#globalLeaderboardTable").hide();
        },
        success: function (data) {
            var i = 0;
            var tabBody = $("#globalLeaderboardTable").find('tbody');
            data.forEach(e => {
                var background = i % 2 == 0 ? "white" : "azure";
                tabBody.append(`<tr style="background-color: ` + background + `">
                        <td style="text-align: center">` + e.rank + `</td>
                        <td style="color: crimson"><a href="/Leaderboard?userId=` + e.userId + `">` + e.userName + `</a></td>
                        <td style="text-align: right">` + e.points + `</td>
                        <td style="text-align: right">` + e.bestTimeString + `</td>
                        <td style="text-align: right">` + e.kikolesFound + `</td>
                        <td style="text-align: right">` + e.kikolesAttempted + `</td>
                        <td style="text-align: right">` + e.kikolesProposed + `</td>
                    </tr>`);
                i++;
            });
            if (i == 0) {
                tabBody.append('<tr style="background-color: white"><td colspan="7">' + noUserInTableText + '</td></tr>');
            }
            $("#loading-image").hide();
            $("#globalLeaderboardTable").show();
        },
        error: function (data) {
            alert('Call error: ' + JSON.stringify(data));
        }
    });
};

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
            return false;
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
            $("#countryName").val(i.item.label);
            if ($("#submitCountry").length > 0) {
                $("#submitCountry").click();
            }
            return false;
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
            return false;
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
            return false;
        },
        minLength: 1
    });
};

/* collapsible blocks management  */
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

function drawStatisticPageCharts() {

    var playerDistributionCountryDatas = [['Country', 'Players percent']];
    var playerDistributionPositionDatas = [['Position', 'Players percent']];
    var playerDistributionDecadeDatas = [['Decade', 'Players percent']];
    var playerDistributionClubDatas = [['Club', 'Players count']];
    $.ajax({
        url: '/Leaderboard/GetStatisticPlayersDistribution/',
        data: {},
        type: "GET",
        async: false,
        success: function (data) {
            data.country.forEach(item => playerDistributionCountryDatas.push([item.Key, item.Value]));
            data.position.forEach(item => playerDistributionPositionDatas.push([item.Key, item.Value]));
            data.decade.forEach(item => playerDistributionDecadeDatas.push([item.Key, item.Value]));
            data.club.forEach(item => playerDistributionClubDatas.push([item.Key, item.Value]));
        }
    });
    buildPlayerDistributionPieChartGraph('playerDistributionCountryChart', playerDistributionCountryDatas, 'Distribution by country');
    buildPlayerDistributionPieChartGraph('playerDistributionPositionChart', playerDistributionPositionDatas, 'Distribution by position');
    buildPlayerDistributionPieChartGraph('playerDistributionDecadeChart', playerDistributionDecadeDatas, 'Distribution by decade');
    buildPlayerDistributionColumnChartGraph('playerDistributionClubChart', playerDistributionClubDatas, 'Top 25 clubs');

    var weekActivityDatas = [['Week', 'Players']];
    var monthActivityDatas = [['Month', 'Players']];
    var dayActivityDatas = [['Day', 'Players']];
    $.ajax({
        url: '/Leaderboard/GetStatisticActiveUsers/',
        data: {},
        type: "GET",
        async: false,
        success: function (data) {
            data.weekly.forEach(item => weekActivityDatas.push([item.Key, item.Value]));
            data.monthly.forEach(item => monthActivityDatas.push([item.Key, item.Value]));
            data.daily.forEach(item => dayActivityDatas.push([item.Key, item.Value]));
        }
    });
    buildActiveUsersLineChartGraph('dayActiveUsersChart', dayActivityDatas, 'Date');
    buildActiveUsersLineChartGraph('weekActiveUsersChart', weekActivityDatas, 'Week');
    buildActiveUsersLineChartGraph('monthActiveUsersChart', monthActivityDatas, 'Month');
}

function buildActiveUsersLineChartGraph(elementId, sourceDatas, yAxisTitle) {
    var tableDats = google.visualization.arrayToDataTable(sourceDatas);
    var options = {
        hAxis: { title: yAxisTitle },
        vAxis: { title: 'Active users' },
        legend: 'none',
        width: 1600,
        height: 900
    };
    new google.visualization
        .LineChart(document.getElementById(elementId))
        .draw(tableDats, options);
}

function buildPlayerDistributionPieChartGraph(elementId, sourceDatas, pieTitle) {
    var data = google.visualization.arrayToDataTable(sourceDatas);
    var options = {
        title: pieTitle,
        width: 1600,
        height: 900
    };
    new google.visualization
        .PieChart(document.getElementById(elementId))
        .draw(data, options);
}

function buildPlayerDistributionColumnChartGraph(elementId, sourceDatas, title) {
    var data = google.visualization.arrayToDataTable(sourceDatas);
    var options = {
        title: title,
        width: 1600,
        height: 900
    };
    new google.visualization
        .ColumnChart(document.getElementById(elementId))
        .draw(data, options);
}

function treatAsUTC(date) {
    var result = new Date(date);
    result.setMinutes(result.getMinutes() - result.getTimezoneOffset());
    return result;
}

function daysBetween(startDate, endDate) {
    var millisecondsPerDay = 24 * 60 * 60 * 1000;
    var endDateReal = treatAsUTC(endDate);
    var startDateReal = treatAsUTC(startDate);
    if (endDateReal > startDateReal) {
        return Math.trunc((endDateReal - startDateReal) / millisecondsPerDay);
    } else {
        return Math.floor((endDateReal - startDateReal) / millisecondsPerDay);
    }
}

function navigateDaysHandler(e) {
    window.location.href = "/?day=" + daysBetween(e.target.value, Date.now());
}