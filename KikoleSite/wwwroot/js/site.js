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

/* bouton "oeil" des champs mot de passe (Account/Index.cshtml, Account/ResetPassword.cshtml) :
   bascule permanente affiche/masque (pas un maintien) - plus confortable a relire. */
$(function () {
    $(".password-toggle").on("click", function () {
        var $btn = $(this);
        var $input = $btn.siblings("input");
        var showing = $input.attr("type") === "text";
        $input.attr("type", showing ? "password" : "text");
        $btn.toggleClass("showing", !showing);
    });
});

/* bandeau d'annonce admin (Partial/Announcement, Home/Index.cshtml) : l'etat
   replie/deplie persiste (localStorage, cle par id de message) pour survivre a un
   changement de page - un futur message (autre id) redemarre toujours deplie.
   "ne plus afficher" persiste l'id du message dans un cookie pour ne pas le
   remontrer aux prochaines visites - un futur message (autre id) n'est donc jamais
   masque par erreur. */
$(function () {
    var $announcement = $(".kikole-board .announcement");
    if ($announcement.length === 0) {
        return;
    }

    var messageId = $announcement.data("messageId");
    var collapseStorageKey = "kikoleCollapsedAnnouncement";
    var $toggle = $announcement.find(".announcement-toggle");

    var storedCollapsedId = null;
    try {
        storedCollapsedId = localStorage.getItem(collapseStorageKey);
    } catch (e) { /* stockage indisponible (navigation privee, etc.) */ }
    if (messageId && storedCollapsedId === String(messageId)) {
        $announcement.addClass("collapsed");
        $toggle.attr("aria-expanded", "false");
    }

    $toggle.on("click", function () {
        var collapsed = $announcement.toggleClass("collapsed").hasClass("collapsed");
        $(this).attr("aria-expanded", collapsed ? "false" : "true");
        try {
            if (collapsed && messageId) {
                localStorage.setItem(collapseStorageKey, String(messageId));
            } else {
                localStorage.removeItem(collapseStorageKey);
            }
        } catch (e) { /* stockage indisponible */ }
    });

    $announcement.find(".announcement-dismiss").on("click", function () {
        if (messageId) {
            var cookieName = "kikoleDismissedAnnouncements";
            var existingRow = document.cookie.split("; ").find(function (row) {
                return row.indexOf(cookieName + "=") === 0;
            });
            var ids = existingRow ? existingRow.split("=")[1].split(",") : [];
            if (ids.indexOf(String(messageId)) === -1) {
                ids.push(String(messageId));
            }
            var expires = new Date();
            expires.setFullYear(expires.getFullYear() + 1);
            document.cookie = cookieName + "=" + ids.join(",") + "; expires=" + expires.toUTCString() + "; path=/";
            try {
                if (localStorage.getItem(collapseStorageKey) === String(messageId)) {
                    localStorage.removeItem(collapseStorageKey);
                }
            } catch (e) { /* stockage indisponible */ }
        }
        $announcement.remove();
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

/* validation cote client du nom de joueur (Home/Index.cshtml) : le serveur refusait deja
   une valeur vide (HomeController.IsValidInput), mais seulement apres un aller-retour
   complet. Bloque ici la soumission avant meme la requete, sans toucher au "Devoiler"
   (autre formulaire, cible via form="giveUpForm"). */
$(function () {
    var $form = $("#playerNameForm");
    if ($form.length === 0) return;
    var $input = $("#playerNameInput");
    var $error = $("#playerNameError");

    $form.on("submit", function (e) {
        if ($input.val().trim() === "") {
            e.preventDefault();
            $input.addClass("invalid").attr("aria-invalid", "true");
            $error.prop("hidden", false);
            $input.trigger("focus");
        }
    });

    $input.on("input", function () {
        $input.removeClass("invalid").removeAttr("aria-invalid");
        $error.prop("hidden", true);
    });
});

/* popup de victoire (Home/Index.cshtml) : deja rendue ouverte cote serveur quand
   HomeModel.JustWon est vrai (une seule fois, sur la reponse qui fait vraiment
   gagner - jamais sur une simple re-consultation d'un jour deja trouve). Pas de
   bouton pour l'ouvrir ici, juste la fermeture au clic (n'importe ou dessus) et le
   feu d'artifice lance une fois au chargement. */
$(function () {
    var modal = document.getElementById('winModal');
    if (!modal) {
        return;
    }

    modal.addEventListener('click', function (e) {
        // les elements interactifs (ex. "details" d'un badge) ne ferment pas la popup
        if (e.target.closest('summary, details, a, button, input, select, textarea')) {
            return;
        }
        modal.classList.remove('open');
    });

    var canvas = document.getElementById('winModalConfetti');
    if (canvas && canvas.getContext) {
        launchWinConfetti(canvas);
    }
});

/* feu d'artifice de la popup de victoire : une salve centrale puis une dizaine de bouquets
   lances a des endroits aleatoires (particules a traînee lumineuse, qui retombent), plus
   une pluie de confettis. Le canvas est devant la popup (cf. .win-modal-confetti) et laisse
   passer les clics. Rien n'est anime si l'utilisateur a demande moins de mouvement. */
var launchWinConfetti = function (canvas) {
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        return;
    }

    var ctx = canvas.getContext('2d');
    var dpr = 1; // resolution native ignoree : un canvas plein ecran en HiDPI ralentit les vieux postes

    var resize = function () {
        canvas.width = canvas.offsetWidth * dpr;
        canvas.height = canvas.offsetHeight * dpr;
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };
    resize();
    window.addEventListener('resize', resize);

    var colors = ['#e8b44c', '#f7d774', '#35d07f', '#ff7a6b', '#f2f4f8', '#6aa5ff', '#c58bff'];
    var pick = function () { return colors[Math.floor(Math.random() * colors.length)]; };
    var sparks = [];
    var confetti = [];

    var burst = function (x, y, count, power) {
        var main = pick();
        var alt = pick();
        for (var i = 0; i < count; i++) {
            var angle = (i / count) * Math.PI * 2 + Math.random() * 0.25;
            var speed = power * (0.45 + Math.random() * 0.55);
            sparks.push({
                x: x, y: y, px: x, py: y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                age: 0,
                ttl: 50 + Math.random() * 35,
                size: 1.6 + Math.random() * 2.2,
                color: Math.random() < 0.78 ? main : alt
            });
        }
    };

    var shower = function (count) {
        for (var i = 0; i < count; i++) {
            confetti.push({
                x: Math.random() * canvas.offsetWidth,
                y: -12 - Math.random() * canvas.offsetHeight * 0.25,
                vx: (Math.random() - 0.5) * 2.2,
                vy: 2 + Math.random() * 3.2,
                size: 6 + Math.random() * 6,
                color: pick(),
                rotation: Math.random() * Math.PI * 2,
                rotationSpeed: (Math.random() - 0.5) * 0.35,
                age: 0,
                ttl: 130 + Math.random() * 60
            });
        }
    };

    // instants (ms) des salves ; la premiere, plus grosse, part du centre
    var schedule = [0, 450, 950, 1450, 2000, 2600];
    var next = 0;
    var startTime = Date.now();

    var frame = function () {
        var elapsed = Date.now() - startTime;
        var w = canvas.offsetWidth;
        var h = canvas.offsetHeight;
        ctx.clearRect(0, 0, w, h);

        while (next < schedule.length && elapsed >= schedule[next]) {
            if (next === 0) {
                burst(w / 2, h * 0.42, 85, 11);
                shower(26);
            } else {
                burst(w * (0.12 + Math.random() * 0.76), h * (0.14 + Math.random() * 0.42), 50, 8);
                if (next % 2 === 1) {
                    shower(16);
                }
            }
            next++;
        }

        ctx.lineCap = 'round';
        sparks = sparks.filter(function (p) { return p.age < p.ttl; });
        sparks.forEach(function (p) {
            p.px = p.x;
            p.py = p.y;
            p.vx *= 0.985;
            p.vy = p.vy * 0.985 + 0.1;
            p.x += p.vx;
            p.y += p.vy;
            p.age++;

            var life = 1 - p.age / p.ttl;
            ctx.strokeStyle = p.color;
            // halo : meme trait en plus large et translucide (beaucoup moins cher qu'un shadowBlur)
            ctx.globalAlpha = Math.max(0, life) * 0.22;
            ctx.lineWidth = p.size * (0.5 + life * 0.7) * 3.2;
            ctx.beginPath();
            ctx.moveTo(p.px, p.py);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
            ctx.globalAlpha = Math.max(0, life);
            ctx.lineWidth = p.size * (0.5 + life * 0.7);
            ctx.beginPath();
            ctx.moveTo(p.px, p.py);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();
        });

        confetti = confetti.filter(function (p) { return p.age < p.ttl && p.y < h + 30; });
        confetti.forEach(function (p) {
            p.vy = Math.min(p.vy + 0.02, 5.5);
            p.x += p.vx + Math.sin(p.age / 11) * 0.7;
            p.y += p.vy;
            p.rotation += p.rotationSpeed;
            p.age++;

            ctx.save();
            ctx.globalAlpha = Math.max(0, Math.min(1, (p.ttl - p.age) / 40));
            ctx.translate(p.x, p.y);
            ctx.rotate(p.rotation);
            ctx.fillStyle = p.color;
            ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2);
            ctx.restore();
        });

        ctx.globalAlpha = 1;

        if (next < schedule.length || sparks.length > 0 || confetti.length > 0) {
            requestAnimationFrame(frame);
        } else {
            ctx.clearRect(0, 0, w, h);
            window.removeEventListener('resize', resize);
        }
    };

    requestAnimationFrame(frame);
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

var initializeLeaderboards = function (noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText, badgesFoundHeader, badgesMissingHeader, badgePercentageHeader, averageRarityHeader) {
    paginateTable(document.getElementById('globalLeaderboardTable'));
    paginateTable(document.getElementById('dailyLeaderboardTable'));
    paginateTableByGroup(document.getElementById('monthlyPodiumTable'));
    paginateTable(document.getElementById('overallPodiumTable'));

    /* global */
    var sortType = document.getElementById('SortType');
    var fromDate = document.getElementById('MinimalDate');
    var toDate = document.getElementById('MaximalDate');

    /* l'entete "normale" (Points/Temps min./Trouves/...) n'existe qu'une fois, rendue par
       le serveur : on la capture avant de jamais la modifier, pour pouvoir la restaurer
       quand on quitte le tri "% de badges" (seul tri dont les colonnes different). */
    var globalLeaderboardTableEl = document.getElementById('globalLeaderboardTable');
    globalLeaderboardTableEl.dataset.defaultHeaderHtml = globalLeaderboardTableEl.tHead.rows[0].innerHTML;

    var badgeHeaders = {
        found: badgesFoundHeader,
        missing: badgesMissingHeader,
        percentage: badgePercentageHeader,
        rarity: averageRarityHeader
    };

    /* le % de badges est un cumul sur toute la partie, sans notion de periode : les
       filtres de dates n'ont pas de sens pour ce tri, donc desactives plutot que
       masques (la mise en page du formulaire reste stable). */
    var syncGlobalLeaderboardDateFields = function () {
        var isBadgeMode = sortType.value === 'BadgePercentage';
        fromDate.disabled = isBadgeMode;
        toDate.disabled = isBadgeMode;
    };

    sortType.onchange = function () {
        syncGlobalLeaderboardDateFields();
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, currentUserId, badgeHeaders);
    };
    fromDate.onchange = function () {
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, currentUserId, badgeHeaders);
    };
    toDate.onchange = function () {
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, currentUserId, badgeHeaders);
    };

    /* daily */
    var dailySortType = document.getElementById('DaySortType');
    var dailyDate = document.getElementById('LeaderboardDay');
    dailySortType.onchange = function () {
        loadDailyLeaderboard(dailySortType.value, dailyDate.value, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText);
    };
    dailyDate.onchange = function () {
        loadDailyLeaderboard(dailySortType.value, dailyDate.value, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText);
    };

    /* bouton "Decouvrez le classement" affiche quand le tableau du jour est masque
       (rendu cote serveur au premier chargement, recree en JS a chaque rafraichissement
       du tableau tant qu'il reste masque, cf. loadDailyLeaderboard) : delegue sur
       document plutot que lie directement, pour continuer a fonctionner apres que le
       bouton a ete recree. */
    $(document).off('click.unlockDailyLeaderboard').on('click.unlockDailyLeaderboard', '#unlockDailyLeaderboardBtn', function () {
        buyDailyLeaderboardAccess(dailySortType.value, dailyDate.value, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText);
    });

    /* navigation arriere (bouton "precedent" du navigateur) : certains navigateurs
       restaurent la valeur affichee d'un champ de formulaire independamment du contenu
       de la page, qui lui reste celui fige au rendu serveur d'origine - le filtre
       affiche et le tableau visible peuvent alors ne plus correspondre (onchange ne se
       declenche pas pour une restauration programmatique). On resynchronise des que ca
       arrive. */
    if (isControlValueStale(sortType) || isControlValueStale(fromDate) || isControlValueStale(toDate)) {
        syncGlobalLeaderboardDateFields();
        loadGlobalLeaderboard(sortType.value, fromDate.value, toDate.value, noUserInTableText, currentUserId, badgeHeaders);
    }
    if (isControlValueStale(dailySortType) || isControlValueStale(dailyDate)) {
        loadDailyLeaderboard(dailySortType.value, dailyDate.value, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText);
    }
};

/* compare la valeur actuelle d'un champ a celle rendue par le serveur (attribut HTML
   d'origine, jamais modifie par le navigateur lui-meme) : differentes si un retour
   arriere a restaure une valeur posterieure au rendu de la page. */
var isControlValueStale = function (control) {
    if (control.tagName === 'SELECT') {
        var renderedOption = control.querySelector('option[selected]');
        return !!renderedOption && renderedOption.value !== control.value;
    }
    return control.getAttribute('value') !== control.value;
};

/* cellule "utilisateur" partagee par les lignes de tableau regenerees en AJAX
   ci-dessous : ajoute le lien, et marque la ligne de l'utilisateur connecte (classe
   .you-row sur le <tr>, mise en forme dans kikole-board.css ; cf.
   Views/Leaderboard/Index.cshtml pour l'equivalent cote rendu serveur). */
var appendUsernameCell = function (row, userId, userName, href, currentUserId) {
    var newCell = row.insertCell();
    var userLink = document.createElement('a');
    userLink.href = href;
    userLink.append(document.createTextNode(userName));
    newCell.appendChild(userLink);
    newCell.classList.add('tabData');
    newCell.classList.add('redtext');
    if (currentUserId && String(userId) === String(currentUserId)) {
        newCell.classList.add('you');
        row.classList.add('you-row');
    }
    return newCell;
};

/* cellule texte simple, factorisee pour les deux variantes de colonnes du classement
   general (normale et "% de badges", cf. loadGlobalLeaderboard). */
var appendTextCell = function (row, text) {
    var newCell = row.insertCell();
    newCell.appendChild(document.createTextNode(text));
    newCell.classList.add('tabData');
    return newCell;
};

/* pagination cote navigateur des tableaux : les donnees arrivent deja completes (rendu
   serveur ou appel AJAX), on masque simplement les lignes hors page ; a rappeler apres
   chaque remplacement du <tbody>. Fleches uniquement, sans numero de page. Libelles des
   fleches : data-prev-label / data-next-label sur le .table-wrap (localises par la vue). */
var LEADERBOARD_PAGE_SIZE = 25;

/* barre de fleches placee sous le tableau (remplace l'eventuelle precedente) */
var createPager = function (table) {
    var wrap = table.closest('.table-wrap');
    var old = wrap.parentNode.querySelector('.pager[data-for="' + table.id + '"]');
    if (old) {
        old.remove();
    }

    var chevron = function (d) {
        return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="' + d + '"/></svg>';
    };
    var pager = document.createElement('div');
    pager.className = 'pager';
    pager.setAttribute('data-for', table.id);
    pager.innerHTML =
        '<button type="button" class="pager-btn" title="' + wrap.dataset.prevLabel + '" aria-label="' + wrap.dataset.prevLabel + '">' + chevron('M15 6l-6 6 6 6') + '</button>' +
        '<span class="pager-range"></span>' +
        '<button type="button" class="pager-btn" title="' + wrap.dataset.nextLabel + '" aria-label="' + wrap.dataset.nextLabel + '">' + chevron('M9 6l6 6-6 6') + '</button>';
    wrap.insertAdjacentElement('afterend', pager);

    var buttons = pager.querySelectorAll('.pager-btn');
    return { pager: pager, prev: buttons[0], next: buttons[1], label: pager.querySelector('.pager-range') };
};

/* alternance de couleurs recalculee sur les seules lignes visibles */
var restripe = function (rows) {
    var zebra = 0;
    rows.forEach(function (r) {
        if (!r.hidden && !r.classList.contains('creator')) {
            r.classList.remove('even', 'odd');
            r.classList.add(zebra % 2 === 0 ? 'even' : 'odd');
            zebra++;
        }
    });
};

/* 25 lignes par page (classements) */
var paginateTable = function (table) {
    var rows = Array.prototype.slice.call(table.tBodies[0].rows);
    rows.forEach(function (r) { r.hidden = false; });
    var old = table.closest('.table-wrap').parentNode.querySelector('.pager[data-for="' + table.id + '"]');
    if (old) {
        old.remove();
    }
    if (rows.length <= LEADERBOARD_PAGE_SIZE) {
        return;
    }

    var ui = createPager(table);
    var pageCount = Math.ceil(rows.length / LEADERBOARD_PAGE_SIZE);
    var page = 0;

    var show = function () {
        var start = page * LEADERBOARD_PAGE_SIZE;
        var end = Math.min(start + LEADERBOARD_PAGE_SIZE, rows.length);
        rows.forEach(function (r, i) { r.hidden = !(i >= start && i < end); });
        restripe(rows);
        ui.label.textContent = (start + 1) + '\u2013' + end + ' / ' + rows.length;
        ui.prev.disabled = page === 0;
        ui.next.disabled = page === pageCount - 1;
    };

    ui.prev.addEventListener('click', function () { page = Math.max(0, page - 1); show(); });
    ui.next.addEventListener('click', function () { page = Math.min(pageCount - 1, page + 1); show(); });
    show();
};

/* un groupe de lignes par page (mois d'une fiche joueur, annee du podium mensuel, plus recent d'abord) : chaque
   <tr> porte data-group ("2026-09") et data-group-label (libelle affiche). La fleche de
   gauche remonte dans le temps (groupe plus ancien), celle de droite revient vers le present. */
var paginateTableByGroup = function (table) {
    var rows = Array.prototype.slice.call(table.tBodies[0].rows);
    var months = [];
    rows.forEach(function (r) {
        var m = r.getAttribute('data-group');
        if (m && months.indexOf(m) < 0) {
            months.push(m);
        }
    });
    if (months.length <= 1) {
        return;
    }

    var ui = createPager(table);
    var index = 0;

    var show = function () {
        var current = months[index];
        var label = '';
        rows.forEach(function (r) {
            r.hidden = r.getAttribute('data-group') !== current;
            if (!r.hidden && !label) {
                label = r.getAttribute('data-group-label');
            }
        });
        restripe(rows);
        ui.label.textContent = label;
        ui.prev.disabled = index === months.length - 1;
        ui.next.disabled = index === 0;
    };

    ui.prev.addEventListener('click', function () { index = Math.min(months.length - 1, index + 1); show(); });
    ui.next.addEventListener('click', function () { index = Math.max(0, index - 1); show(); });
    show();
};

$(function () {
    var byDay = document.getElementById('userStatsByDayTable');
    if (byDay) {
        paginateTableByGroup(byDay);
    }
});
/* leaderboard loading */

/* le tri "% de badges" (cumul sur toute la partie, sans notion de periode) n'affiche pas
   les memes colonnes que les autres : Rang/Joueur restent communs, le reste (Points/Temps
   min./Trouves/Tentes/Proposes vs Badges trouves/manquants/%/Rarete moy.) est entierement
   remplace, entete comprise (cf. badgeHeaders, capture dans initializeLeaderboards). */
var loadGlobalLeaderboard = function (sortType, dateMin, dateMax, noUserInTableText, currentUserId, badgeHeaders) {
    var isBadgeMode = sortType === 'BadgePercentage';
    if (!isBadgeMode && (!dateMin || !dateMax)) {
        return;
    }
    var url = '/global-leaderboard-details?sortType=' + sortType +
        '&minimalDate=' + (dateMin || '1970-01-01') + '&maximalDate=' + (dateMax || '1970-01-01');
    $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        success: function (data) {
            var table = document.getElementById('globalLeaderboardTable');
            var tbodyRef = table.getElementsByTagName('tbody')[0];
            var newtbody = document.createElement('tbody');

            var headRow = table.tHead.rows[0];
            if (isBadgeMode) {
                var posHtml = headRow.cells[0].outerHTML;
                var usernameHtml = headRow.cells[1].outerHTML;
                headRow.innerHTML = posHtml + usernameHtml +
                    '<th class="tabDataHead">' + badgeHeaders.found + '</th>' +
                    '<th class="tabDataHead">' + badgeHeaders.missing + '</th>' +
                    '<th class="tabDataHead">' + badgeHeaders.percentage + '</th>' +
                    '<th class="tabDataHead">' + badgeHeaders.rarity + '</th>';
            } else {
                headRow.innerHTML = table.dataset.defaultHeaderHtml;
            }

            var colSpan = isBadgeMode ? 6 : 7;
            var i = 0;
            data.forEach(e => {
                var trClass = i % 2 == 0 ? "even" : "odd";
                var newRow = newtbody.insertRow();
                newRow.classList.add(trClass);

                appendTextCell(newRow, e.rank);
                appendUsernameCell(newRow, e.userId, e.userName, '/Leaderboard?userId=' + e.userId, currentUserId);

                if (isBadgeMode) {
                    appendTextCell(newRow, e.badgesFound);
                    appendTextCell(newRow, e.badgesMissing);
                    appendTextCell(newRow, e.badgePercentageString);
                    appendTextCell(newRow, e.averageBadgeRarityString);
                } else {
                    appendTextCell(newRow, e.points);
                    appendTextCell(newRow, e.bestTimeString);
                    appendTextCell(newRow, e.kikolesFound);
                    appendTextCell(newRow, e.kikolesAttempted);
                    appendTextCell(newRow, e.kikolesProposed);
                }

                i++;
            });
            if (i == 0) {
                var newRow = newtbody.insertRow();
                newRow.classList.add('even');
                appendTextCell(newRow, noUserInTableText).colSpan = colSpan;
            }
            table.replaceChild(newtbody, tbodyRef);
            paginateTable(table);
        },
        error: function (data) {
            alert('Call error: ' + JSON.stringify(data));
        }
    });
};

var loadDailyLeaderboard = function (sortType, date, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText) {
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

                    appendUsernameCell(newRow, e.userId, e.userName, '/Leaderboard?userId=' + e.userId, currentUserId);

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(e.timeString);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(e.points);
                    if (e.isCreator || !data.canViewDetails) {
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

                    appendUsernameCell(newRow, e.userId, e.userName, '/Leaderboard?userId=' + e.userId, currentUserId);

                    var newCell = newRow.insertCell();
                    var newText = document.createTextNode(noTimeYetText);
                    newCell.appendChild(newText);
                    newCell.classList.add('tabData');

                    var newCell = newRow.insertCell();
                    if (data.canViewDetails) {
                        var userLink = document.createElement('a');
                        userLink.href = '/Leaderboard/UserDay?userId=' + e.userId + '&date=' + data.date;
                        userLink.append(document.createTextNode('(' + e.points + ')'));
                        newCell.appendChild(userLink);
                    } else {
                        newCell.appendChild(document.createTextNode('(' + e.points + ')'));
                    }
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
                newCell.classList.add('tabData');
                newCell.classList.add('hidden-board-cell');
                newCell.colSpan = 4;

                var messageDiv = document.createElement('div');
                messageDiv.appendChild(document.createTextNode(hiddenBoardText));
                newCell.appendChild(messageDiv);

                if (currentUserId) {
                    var actionsDiv = document.createElement('div');
                    actionsDiv.classList.add('actions');
                    actionsDiv.classList.add('daily-unlock-actions');

                    var unlockButton = document.createElement('button');
                    unlockButton.type = 'button';
                    unlockButton.id = 'unlockDailyLeaderboardBtn';
                    unlockButton.appendChild(document.createTextNode(discoverLeaderboardText + ' '));

                    var costSpan = document.createElement('span');
                    costSpan.classList.add('cost');
                    costSpan.appendChild(document.createTextNode(leaderboardCostText));
                    unlockButton.appendChild(costSpan);

                    actionsDiv.appendChild(unlockButton);
                    newCell.appendChild(actionsDiv);
                }
            }
            
            table.replaceChild(newtbody, tbodyRef);
            paginateTable(table);
        },
        error: function (data) {
            alert('Call error: ' + JSON.stringify(data));
        }
    });
};

/* achat du classement du jour depuis la page classement elle-meme (bouton "Decouvrez le
   classement" sous le message "resultats masques") : meme action que le bouton "Acces au
   classement du jour" de la page d'accueil, cote serveur - la page ne change pas, seul le
   tableau quotidien est rafraichi une fois l'achat effectue. */
var buyDailyLeaderboardAccess = function (sortType, date, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText) {
    $.ajax({
        url: '/unlock-daily-leaderboard',
        type: 'POST',
        success: function () {
            loadDailyLeaderboard(sortType, date, noUserInTableText, noTimeYetText, noPointsYetText, hiddenBoardText, currentUserId, discoverLeaderboardText, leaderboardCostText);
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
    var maxYear = new Date().getFullYear() - 10;
    for (var i = 1850; i <= maxYear; i++)
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
                            label: item.display,
                            value: item.key,
                            canonical: item.value
                        };
                    }))
                }
            });
        },
        select: function (e, i) {
            $(idFieldId).val(i.item.value);
            $(nameFieldId).val(i.item.canonical);
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

/* google.charts dessine par defaut sur fond blanc avec du texte sombre : options communes
   pour rester dans la charte sombre (fond transparent, texte et grilles clairs, series
   a fort contraste). */
function darkChartOptions(extra) {
    var text = { color: '#c9d0dd', fontName: 'IBM Plex Sans', fontSize: 12 };
    var base = {
        backgroundColor: 'transparent',
        colors: ['#35d07f', '#e8b44c', '#ff7a6b', '#6aa5ff', '#b08cff', '#f08bd0', '#5fd6d6', '#ff9f43'],
        titleTextStyle: { color: '#f2f4f8', fontName: 'IBM Plex Sans', fontSize: 14, bold: true },
        legend: { textStyle: text },
        hAxis: { textStyle: text, titleTextStyle: text, gridlines: { color: '#2a3140' }, baselineColor: '#3f4a5f' },
        vAxis: { textStyle: text, titleTextStyle: text, gridlines: { color: '#2a3140' }, baselineColor: '#3f4a5f' },
        pieSliceBorderColor: '#141922',
        pieSliceTextStyle: { color: '#06210f' },
        tooltip: { textStyle: { color: '#141922' } },
        width: '100%',
        height: 360
    };
    for (var key in extra) {
        base[key] = (typeof extra[key] === 'object' && base[key] && !Array.isArray(extra[key]))
            ? Object.assign({}, base[key], extra[key])
            : extra[key];
    }
    return base;
}

function buildActiveUsersLineChartGraph(elementId, sourceDatas, yAxisTitle) {
    if (!hasChartData(sourceDatas)) {
        showNoChartData(elementId);
        return;
    }
    var tableDats = google.visualization.arrayToDataTable(sourceDatas);
    var options = darkChartOptions({
        hAxis: { title: yAxisTitle },
        vAxis: { title: 'Active users' },
        legend: 'none'
    });
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
    var options = darkChartOptions({ title: pieTitle });
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
    var options = darkChartOptions({ title: title });
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

/* listes deroulantes personnalisees : remplace l'affichage des <select class="blank"> par un
   bouton + un panneau dont les elements ont le meme style que la valeur selectionnee. Le
   <select> natif reste dans le DOM (cache) et garde la valeur : formulaires et handlers
   "change" existants continuent de fonctionner tels quels. */
$(function () {
    var closeAll = function (except) {
        $(".dd.open").not(except).removeClass("open").find(".dd-btn").attr("aria-expanded", "false");
    };

    $("select.blank").each(function () {
        var select = this;
        var $dd = $('<div class="dd"></div>');
        var $btn = $('<button type="button" class="dd-btn blank" aria-haspopup="listbox" aria-expanded="false"></button>');
        var $list = $('<ul class="dd-list" role="listbox"></ul>');

        var refresh = function () {
            var opt = select.options[select.selectedIndex];
            $btn.text(opt && opt.text ? opt.text : "—").toggleClass("placeholder", !opt || opt.value === "0" || opt.value === "");
            $list.children().each(function (i) {
                $(this).toggleClass("selected", i === select.selectedIndex).attr("aria-selected", i === select.selectedIndex);
            });
        };

        var choose = function (index) {
            if (select.selectedIndex !== index) {
                select.selectedIndex = index;
                select.dispatchEvent(new Event("change", { bubbles: true }));
            }
            refresh();
            closeAll();
            $btn.focus();
        };

        $.each(select.options, function (i, o) {
            var $li = $('<li class="dd-item" role="option"></li>').text(o.text || "—");
            if (o.value === "0" || o.value === "") $li.addClass("placeholder");
            $li.on("click", function () { choose(i); });
            $list.append($li);
        });

        $btn.on("click", function () {
            var willOpen = !$dd.hasClass("open");
            closeAll($dd);
            $dd.toggleClass("open", willOpen);
            $btn.attr("aria-expanded", willOpen ? "true" : "false");
        });

        $btn.on("keydown", function (e) {
            if (e.key === "Escape") { closeAll(); return; }
            if (e.key === "ArrowDown" || e.key === "ArrowUp") {
                e.preventDefault();
                var next = Math.max(0, Math.min(select.options.length - 1, select.selectedIndex + (e.key === "ArrowDown" ? 1 : -1)));
                choose(next);
            }
        });

        $(select).addClass("dd-native").attr("tabindex", "-1").attr("aria-hidden", "true");
        $dd.insertBefore(select).append($btn, $list, select);
        $(select).on("change", refresh);
        refresh();
    });

    $(document).on("click", function (e) {
        if (!$(e.target).closest(".dd").length) closeAll();
    });
});

/* upload des medias d'indice (Admin/Index.cshtml, Admin/PlayerEdit.cshtml) : chaque champ
   texte de clue/easy clue (".clue-field") recoit un input file adjacent - au choix d'un
   fichier, upload immediat via AdminController.UploadClueMedia, puis le chemin renvoye
   remplace la valeur du champ texte (la saisie manuelle d'une URL reste possible en
   parallele, rien n'empeche de taper directement dans le champ). */
$(function () {
    var $fields = $(".clue-field");
    if ($fields.length === 0) return;

    var imageAccept = ".png,.jpg,.jpeg,.gif,.webp,.bmp,.svg";
    var allAccept = imageAccept + ",.mp3,.mp4";

    $fields.each(function () {
        var $text = $(this);
        var isImageOnly = $text.attr("data-media") === "image";
        var $status = $('<span class="clue-upload-status"></span>');
        var $file = $('<input type="file" class="clue-upload-input" />').attr("accept", isImageOnly ? imageAccept : allAccept);

        $text.after($status).after($file);

        $file.on("change", function () {
            var file = this.files && this.files[0];
            if (!file) return;

            $status.text("Envoi en cours...").removeClass("error success");

            var formData = new FormData();
            formData.append("file", file);

            $.ajax({
                url: "/Admin/UploadClueMedia",
                type: "POST",
                data: formData,
                processData: false,
                contentType: false,
                success: function (data) {
                    $text.val(data.path);
                    $status.text("Envoye : " + data.path).addClass("success");
                },
                error: function (xhr) {
                    var message = xhr.status === 403
                        ? "Audio/video reserves aux administrateurs."
                        : "Echec de l'envoi (format ou taille refuses).";
                    $status.text(message).addClass("error");
                }
            });
        });
    });
});

Date.prototype.ddmmyyyy = function () {
    var mm = this.getMonth() + 1; // getMonth() is zero-based
    var dd = this.getDate();
    return [(dd > 9 ? '' : '0') + dd,
        (mm > 9 ? '' : '0') + mm,
        this.getFullYear()
    ].join('/');
};

/* page de gestion des utilisateurs (Admin/Users.cshtml) : une seule modale de
   confirmation pour les trois actions ; data-input dit quel champ elle affiche
   (reason, password ou none). Un champ masque est desactive pour ne pas etre poste. */
var openUserActionModal = function (button) {
    var data = button.dataset;
    var form = document.getElementById('userActionForm');
    form.action = data.action;
    document.getElementById('userActionUserId').value = data.userId;
    document.getElementById('userActionText').textContent = data.confirm;

    var newType = document.getElementById('userActionNewType');
    newType.value = data.newType || '';
    newType.disabled = !data.newType;

    ['reason', 'password', 'login'].forEach(function (name) {
        var field = document.getElementById('userAction' + name.charAt(0).toUpperCase() + name.slice(1) + 'Field');
        var shown = data.input === name;
        field.hidden = !shown;
        field.querySelectorAll('input, textarea').forEach(function (input) {
            input.disabled = !shown;
            input.value = '';
            input.required = shown;
            input.setCustomValidity('');
            if (name === 'password' && input.type === 'text') {
                input.type = 'password';
            }
        });
        field.querySelectorAll('.password-toggle').forEach(function (toggle) {
            toggle.classList.remove('showing');
        });
    });

    form.dataset.expectedLogin = data.login || '';
    document.getElementById('userActionModal').classList.add('open');
    var firstInput = form.querySelector('.form-field:not([hidden]) input, .form-field:not([hidden]) textarea');
    if (firstInput) {
        firstInput.focus();
    }
};

var closeUserActionModal = function () {
    document.getElementById('userActionModal').classList.remove('open');
};

$(function () {
    var $modal = $("#userActionModal");
    if ($modal.length === 0) return;

    $(document).on("keydown", function (e) {
        if (e.key === "Escape") {
            closeUserActionModal();
        }
    });

    // la confirmation doit reprendre le mot de passe : signale l'ecart avant l'envoi
    var $password = $("#userActionPassword");
    var $confirm = $("#userActionPasswordConfirm");
    $password.add($confirm).on("input", function () {
        $confirm[0].setCustomValidity($confirm.val() === $password.val() ? "" : $confirm.data("mismatch"));
    });

    // la suppression exige de retaper le login du compte
    var $loginConfirm = $("#userActionLoginConfirm");
    $loginConfirm.on("input", function () {
        var expected = ($("#userActionForm").data("expectedLogin") || "").toString().toLowerCase();
        $loginConfirm[0].setCustomValidity($loginConfirm.val().trim().toLowerCase() === expected ? "" : $loginConfirm.data("mismatch"));
    });
    var $login = $("#usersLogin");
    $login.autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/Admin/AutoCompleteUserLogins/',
                data: { "prefix": request.term },
                type: "POST",
                success: response
            });
        },
        select: function (e, i) {
            $login.val(i.item.value);
            $("#usersFilterForm").trigger("submit");
            return false;
        },
        minLength: 1
    });
});

/* bouton "Partager mon resultat" (Home/Partial/ShareButton.cshtml) : texte sans spoiler
   (points, serie) + lien du site. Partage natif sur mobile, copie dans le presse-papiers
   ailleurs ; aucun service externe, donc aucun traceur. */
$(function () {
    var feedbackTimer = null;

    var copyWithSelection = function (value) {
        var area = document.createElement('textarea');
        area.value = value;
        area.setAttribute('readonly', '');
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        var copied = false;
        try {
            copied = document.execCommand('copy');
        } catch (err) { /* non disponible */ }
        document.body.removeChild(area);
        return copied;
    };

    // API moderne si possible, sinon copie par selection ; dernier recours : le texte est
    // affiche dans une boite de dialogue pour etre copie a la main
    var copyToClipboard = function (value) {
        var fallback = function () {
            if (!copyWithSelection(value)) {
                window.prompt('', value);
            }
        };
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(value).catch(fallback);
        }
        fallback();
        return Promise.resolve();
    };

    $(document).on("click", ".share-btn", function (e) {
        e.stopPropagation();
        var $btn = $(this);
        var text = $btn.data("shareText");
        var url = $btn.data("shareUrl");
        var $feedback = $btn.siblings(".share-feedback");

        var isTouch = window.matchMedia && window.matchMedia("(pointer: coarse)").matches;
        if (navigator.share && isTouch) {
            navigator.share({ text: text, url: url }).catch(function () { /* partage annule */ });
            return;
        }

        copyToClipboard(text + " " + url).then(function () {
            $feedback.text($btn.data("copiedLabel"));
            clearTimeout(feedbackTimer);
            feedbackTimer = setTimeout(function () { $feedback.text(""); }, 2500);
        });
    });
});