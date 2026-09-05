$(document).ready(function () {
    /* loading google graph lib */
    if (document.getElementById('googleChartEnabler')) {
        google.charts.load('current', { packages: ['corechart'] });
        google.charts.setOnLoadCallback(drawStatisticPageCharts);
    }
});

/* burger menu (nav globale, toutes les pages) */
$(function () {
    $("#siteNavBurger").on("click", function () {
        var open = $("#siteNavDrawer").toggleClass("open").hasClass("open");
        $(this).toggleClass("open", open).attr("aria-expanded", open ? "true" : "false");
    });
});

/* modale de confirmation "Give up" (Home/Index.cshtml) : remplace le confirm() natif
   du navigateur, non personnalisable en CSS. Le bouton declencheur reste un vrai
   type="submit" (pour que son name="submit-GiveUp" soit lu par GetSubmitAction() cote
   serveur) : on bloque juste la soumission tant que la modale n'est pas confirmee. */
var openGiveUpModal = function (event) {
    event.preventDefault();
    document.getElementById('giveUpModal').classList.add('open');
    return false;
};

var closeGiveUpModal = function () {
    document.getElementById('giveUpModal').classList.remove('open');
};

var confirmGiveUp = function () {
    var form = document.getElementById('giveUpForm');
    form.requestSubmit(document.getElementById('giveUpTrigger'));
};

var loadKikolesStats = function (sort, desc) {
    $.ajax({
        url: '/kikoles-stats?sort=' + sort + '&desc=' + desc,
        type: "GET",
        dataType: "json",
        beforeSend: function () {
            $("#loading-image").show();
            $("#kikolesStatsTab").hide();
            $("#sort-block").hide();
        },
        success: function (data) {
            var table = document.getElementById('kikolesStatsTab');
            var tbodyRef = table.getElementsByTagName('tbody')[0];
            var newtbody = document.createElement('tbody');
            var i = 0;
            data.forEach(e => {
                var background = i % 2 == 0 ? "even" : "odd";
                var newRow = newtbody.insertRow();
                newRow.classList.add(background);

                var dateToParse = new Date(Date.parse(e.date));
                var newCell = newRow.insertCell();
                var dayLink = document.createElement('a');
                dayLink.href = '/?day=' + e.daysBefore;
                var newText = document.createTextNode(dateToParse.ddmmyyyy());
                dayLink.appendChild(newText);
                newCell.appendChild(dayLink);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.name);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.creator);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.averagePointsSameDay);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.triesCountSameDay);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.successesCountSameDay);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.averagePointsTotal);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.triesCountTotal);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.successesCountTotal);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.bestTime);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                i++;
            });
            table.replaceChild(newtbody, tbodyRef);
            $("#loading-image").hide();
            $("#kikolesStatsTab").show();
            $("#sort-block").show();
        },
        error: function (data) {
            alert('Call error: ' + JSON.stringify(data));
        }
    });
};

var initializeLeaderboards = function (noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, youText, currentUserId) {
    /* global */
    var sortType = document.getElementById('SortType');
    var fromDate = document.getElementById('MinimalDate');
    var toDate = document.getElementById('MaximalDate');
    sortType.onchange = function () {
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, youText, currentUserId);
    };
    fromDate.onchange = function () {
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, youText, currentUserId);
    };
    toDate.onchange = function () {
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, youText, currentUserId);
    };

    /* daily */
    var dailySortType = document.getElementById('DaySortType');
    var dailyDate = document.getElementById('LeaderboardDay');
    dailySortType.onchange = function () {
        loadDailyLeaderboard(dailySortType.value, dailyDate.value, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, youText, currentUserId);
    };
    dailyDate.onchange = function () {
        loadDailyLeaderboard(dailySortType.value, dailyDate.value, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, youText, currentUserId);
    };
};

/* cellule "utilisateur" partagee par les 3 lignes de tableau regenerees en AJAX
   ci-dessous : ajoute le lien + le petit marqueur "(vous)" quand la ligne est celle
   de l'utilisateur connecte (cf. Views/Leaderboard/Index.cshtml pour l'equivalent
   cote rendu serveur). */
var appendUsernameCell = function (row, userId, userName, href, youText, currentUserId) {
    var newCell = row.insertCell();
    var userLink = document.createElement('a');
    userLink.href = href;
    userLink.append(document.createTextNode(userName));
    newCell.appendChild(userLink);
    newCell.classList.add('tabData');
    newCell.classList.add('redtext');
    if (currentUserId && String(userId) === String(currentUserId)) {
        newCell.classList.add('you');
        var youTag = document.createElement('span');
        youTag.classList.add('you-tag');
        youTag.append(document.createTextNode('(' + youText + ')'));
        newCell.appendChild(youTag);
    }
    return newCell;
};

/* leaderboard loading */
var loadGlobalLeaderboard = function (sortType, dateMin, dateMax, noUserInTableText, youText, currentUserId) {
    if (!dateMin || !dateMax) {
        return;
    }
    $.ajax({
        url: '/global-leaderboard-details?sortType=' + sortType + '&minimalDate=' + dateMin + '&maximalDate=' + dateMax,
        type: "GET",
        dataType: "json",
        success: function (data) {
            var table = document.getElementById('globalLeaderboardTable');
            var tbodyRef = table.getElementsByTagName('tbody')[0];
            var newtbody = document.createElement('tbody');
            var i = 0;
            data.forEach(e => {
                var trClass = i % 2 == 0 ? "even" : "odd";
                var newRow = newtbody.insertRow();
                newRow.classList.add(trClass);

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.rank);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                appendUsernameCell(newRow, e.userId, e.userName, '/Leaderboard?userId=' + e.userId, youText, currentUserId);

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.points);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.bestTimeString);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.kikolesFound);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.kikolesAttempted);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                var newCell = newRow.insertCell();
                var newText = document.createTextNode(e.kikolesProposed);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');

                i++;
            });
            if (i == 0) {
                var newRow = newtbody.insertRow();
                newRow.classList.add('even');
                var newCell = newRow.insertCell();
                var newText = document.createTextNode(noUserInTableText);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');
                newCell.colSpan = 7;
            }
            table.replaceChild(newtbody, tbodyRef);
        },
        error: function (data) {
            alert('Call error: ' + JSON.stringify(data));
        }
    });
};

var loadDailyLeaderboard = function (sortType, date, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, youText, currentUserId) {
    if (!date) {
        return;
    }
    $.ajax({
        url: '/daily-leaderboard-details?sortType=' + sortType + '&date=' + date,
        type: "GET",
        dataType: "json",
        success: function (data) {
            var table = document.getElementById('dailyLeaderboardTable');
            var tbodyRef = table.getElementsByTagName('tbody')[0];
            var newtbody = document.createElement('tbody');
            if (!data.hidden) {
                var i = 0;
                var lastRank = 1;
                data.leaders.forEach(e => {
                    var trClass = i % 2 == 0 ? "even" : "odd";
                    if (e.isCreator) {
                        trClass = 'creator';
                    }

                    var newRow = newtbody.insertRow();
                    newRow.classList.add(trClass);

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(e.rank);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');

                    appendUsernameCell(newRow, e.userId, e.userName, '/Leaderboard?userId=' + e.userId, youText, currentUserId);

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(e.timeString);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(e.points);
                    if (e.isCreator) {
                        newCell.appendChild(newText);
                    } else {
                        var userLink = document.createElement('a');
                        userLink.href = '/Leaderboard/UserDay?userId=' + e.userId + '&date=' + data.date;
                        userLink.append(newText);
                        newCell.appendChild(userLink);
                    }
                    newCell.classList.add('tabData');

                    lastRank = e.rank + 1;
                    i++;
                });

                data.searchers.forEach(e => {
                    var trClass = i % 2 == 0 ? "even" : "odd";
                    var newRow = newtbody.insertRow();
                    newRow.classList.add(trClass);

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(lastRank);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');

                    appendUsernameCell(newRow, e.userId, e.userName, '/Leaderboard?userId=' + e.userId, youText, currentUserId);

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(noTimeYetText);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');

                    var newCell = newRow.insertCell();
                    var userLink = document.createElement('a');
                    userLink.href = '/Leaderboard/UserDay?userId=' + e.userId + '&date=' + data.date;
                    userLink.append(document.createTextNode('(' + e.points + ')'));
                    newCell.appendChild(userLink);
                    newCell.classList.add('tabData');

                    i++;
                });

                if (i == 0) {
                    var newRow = newtbody.insertRow();
                    newRow.classList.add('even');
                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(noUserInTableText);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');
                    newCell.colSpan = 4;
                }
            } else {
                var newRow = newtbody.insertRow();
                newRow.classList.add('even');
                var newCell = newRow.insertCell();
                var newText = document.createTextNode(hiddenBoardText);
                newCell.appendChild(newText);
                newCell.classList.add('tabData');
                newCell.colSpan = 4;
            }
            
            table.replaceChild(newtbody, tbodyRef);
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
var autocompleteCountries = function (nameFieldId, idFieldId, submit) {
    $(nameFieldId).autocomplete({
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
                            label: item.value,
                            value: item.key
                        };
                    }))
                }
            });
        },
        select: function (e, i) {
            $(idFieldId).val(i.item.value);
            $(nameFieldId).val(i.item.label);
            if (submit && $("#submitCountry").length > 0) {
                $("#submitCountry").click();
            }
            return false;
        },
        minLength: 1
    });
};
$(function () {
    autocompleteCountries("#countryName", "#countryId", true);
    if ($("#alternativeCountryName").length > 0) {
        autocompleteCountries("#alternativeCountryName", "#alternativeCountryId", false);
    }
});

/* continents autocompletion */
$(function () {
    $("#continentName").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/Home/AutoCompleteContinents/',
                data: {
                    "prefix": request.term
                },
                type: "POST",
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.value,
                            value: item.key
                        };
                    }))
                }
            });
        },
        select: function (e, i) {
            $("#continentId").val(i.item.value);
            $("#continentName").val(i.item.label);
            if ($("#submitContinent").length > 0) {
                $("#submitContinent").click();
            }
            return false;
        },
        minLength: 1
    });
});

/* clubs autocompletion */
var autocompleteClubs = function (nameFieldId, idFieldId, submit) {
    $(nameFieldId).autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/Home/AutoCompleteClubs/',
                data: {
                    "prefix": request.term
                },
                type: "POST",
                success: function (data) {
                    response($.map(data, function (item) {
                        return {
                            label: item.value,
                            value: item.key
                        };
                    }))
                }
            });
        },
        select: function (e, i) {
            $(idFieldId).val(i.item.value);
            $(nameFieldId).val(i.item.label);
            if (submit && $("#submitClub").length > 0) {
                $("#submitClub").click();
            }
            return false;
        },
        minLength: 1
    });
};
$(function() {
    autocompleteClubs("#clubName", "#clubId", true);
    for (let i = 0; i < 15; i++) {
        autocompleteClubs("#Club" + i, "#Club" + i + "Id", false);
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
        url: '/Statistics/GetStatisticPlayersDistribution/',
        data: {},
        type: "GET",
        async: false,
        success: function (data) {
            data.country.forEach(item => playerDistributionCountryDatas.push([item.key, item.value]));
            data.position.forEach(item => playerDistributionPositionDatas.push([item.key, item.value]));
            data.decade.forEach(item => playerDistributionDecadeDatas.push([item.key, item.value]));
            data.club.forEach(item => playerDistributionClubDatas.push([item.key, item.value]));
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
        url: '/Statistics/GetStatisticActiveUsers/',
        data: {},
        type: "GET",
        async: false,
        success: function (data) {
            data.weekly.forEach(item => weekActivityDatas.push([item.key, item.value]));
            data.monthly.forEach(item => monthActivityDatas.push([item.key, item.value]));
            data.daily.forEach(item => dayActivityDatas.push([item.key, item.value]));
        }
    });
    buildActiveUsersLineChartGraph('dayActiveUsersChart', dayActivityDatas, 'Date');
    buildActiveUsersLineChartGraph('weekActiveUsersChart', weekActivityDatas, 'Week');
    buildActiveUsersLineChartGraph('monthActiveUsersChart', monthActivityDatas, 'Month');
}

/* sourceDatas contient toujours la ligne d'en-tete (cf. drawStatisticPageCharts) : sans
   ligne de donnee derriere, arrayToDataTable ne peut deduire aucun type de colonne et
   google.visualization plante avec "Data column(s) for axis #0 cannot be of type string"
   plutot que d'afficher un graphique vide - un jeu de donnees local (fenetre de dates
   trop recente pour avoir de l'historique, par exemple) tombe facilement dans ce cas. */
function hasChartData(sourceDatas) {
    return sourceDatas.length > 1;
}

function showNoChartData(elementId) {
    var container = document.getElementById(elementId);
    container.textContent = 'No data available yet.';
    container.classList.add('chart-empty');
}

function buildActiveUsersLineChartGraph(elementId, sourceDatas, yAxisTitle) {
    if (!hasChartData(sourceDatas)) {
        showNoChartData(elementId);
        return;
    }
    var tableDats = google.visualization.arrayToDataTable(sourceDatas);
    var options = {
        hAxis: { title: yAxisTitle },
        vAxis: { title: 'Active users' },
        legend: 'none',
        width: '100%',
        height: 360
    };
    new google.visualization
        .LineChart(document.getElementById(elementId))
        .draw(tableDats, options);
}

function buildPlayerDistributionPieChartGraph(elementId, sourceDatas, pieTitle) {
    if (!hasChartData(sourceDatas)) {
        showNoChartData(elementId);
        return;
    }
    var data = google.visualization.arrayToDataTable(sourceDatas);
    var options = {
        title: pieTitle,
        width: '100%',
        height: 360
    };
    new google.visualization
        .PieChart(document.getElementById(elementId))
        .draw(data, options);
}

function buildPlayerDistributionColumnChartGraph(elementId, sourceDatas, title) {
    if (!hasChartData(sourceDatas)) {
        showNoChartData(elementId);
        return;
    }
    var data = google.visualization.arrayToDataTable(sourceDatas);
    var options = {
        title: title,
        width: '100%',
        height: 360
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

/* jQuery UI datepicker : regionalisation partagee FR/EN */
var kikoleDatepickerRegional = {
    fr: {
        closeText: "Fermer",
        prevText: "Préc.",
        nextText: "Suiv.",
        currentText: "Aujourd'hui",
        monthNames: ["janvier", "février", "mars", "avril", "mai", "juin",
            "juillet", "août", "septembre", "octobre", "novembre", "décembre"],
        monthNamesShort: ["janv.", "févr.", "mars", "avr.", "mai", "juin",
            "juil.", "août", "sept.", "oct.", "nov.", "déc."],
        dayNamesMin: ["D", "L", "M", "M", "J", "V", "S"],
        weekHeader: "Sem.",
        dateFormat: "dd/mm/yy",
        firstDay: 1
    },
    en: {
        dateFormat: "mm/dd/yy",
        firstDay: 0
    }
};

/* day navigation datepicker (page d'accueil) : borne a la plage techniquement valide
   (data-min-date/data-max-date, poses par Home/Index.cshtml) plutot que de compter sur
   le serveur pour rattraper un jour hors plage apres coup - evite de proposer un jour
   qui redirigera silencieusement vers aujourd'hui (avant HiddenDate) une fois selectionne. */
var parseIsoDate = function (iso) {
    var parts = iso.split("-");
    return new Date(parts[0], parts[1] - 1, parts[2]);
};

$(function () {
    var lang = document.body.getAttribute("data-lang") === "en" ? "en" : "fr";
    var $dayDatepicker = $("#dayDatepicker");
    var options = $.extend({
        changeMonth: true,
        changeYear: true,
        onSelect: function () {
            var picked = $(this).datepicker("getDate");
            window.location.href = "/?day=" + daysBetween(picked, Date.now());
        }
    }, kikoleDatepickerRegional[lang]);

    var minDate = $dayDatepicker.data("minDate");
    if (minDate) options.minDate = parseIsoDate(minDate);
    var maxDate = $dayDatepicker.data("maxDate");
    if (maxDate) options.maxDate = parseIsoDate(maxDate);

    $dayDatepicker.datepicker(options);
    var initialDate = $dayDatepicker.data("date");
    if (initialDate) {
        $dayDatepicker.datepicker("setDate", parseIsoDate(initialDate));
    }
});

/* datepickers du classement (Leaderboard/Index) : meme widget, mais format ISO
   (yyyy-mm-dd) impose quelle que soit la langue - c'est la valeur brute lue par
   initializeLeaderboards pour les appels AJAX, seuls les libelles du calendrier
   (mois, "aujourd'hui"...) restent localises. jQuery UI ne declenche pas l'evenement
   "change" natif a la selection, d'ou le trigger manuel pour reutiliser les handlers
   deja poses par initializeLeaderboards. */
$(function () {
    var lang = document.body.getAttribute("data-lang") === "en" ? "en" : "fr";
    var $leaderboardDatepickers = $("#LeaderboardDay, #MinimalDate, #MaximalDate");
    var options = $.extend({}, kikoleDatepickerRegional[lang], {
        dateFormat: "yy-mm-dd",
        changeMonth: true,
        changeYear: true,
        onSelect: function () {
            $(this).trigger("change");
        }
    });

    var minDate = $leaderboardDatepickers.data("minDate");
    if (minDate) options.minDate = parseIsoDate(minDate);
    var maxDate = $leaderboardDatepickers.data("maxDate");
    if (maxDate) options.maxDate = parseIsoDate(maxDate);

    $leaderboardDatepickers.datepicker(options);
});

Date.prototype.yyyymmdd = function () {
    var mm = this.getMonth() + 1; // getMonth() is zero-based
    var dd = this.getDate();
    return [this.getFullYear(),
        (mm > 9 ? '' : '0') + mm,
        (dd > 9 ? '' : '0') + dd
    ].join('-');
};

Date.prototype.ddmmyyyy = function () {
    var mm = this.getMonth() + 1; // getMonth() is zero-based
    var dd = this.getDate();
    return [(dd > 9 ? '' : '0') + dd,
        (mm > 9 ? '' : '0') + mm,
        this.getFullYear()
    ].join('/');
};