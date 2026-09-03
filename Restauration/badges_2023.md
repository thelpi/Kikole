# Badges : libelles d'epoque

Extraits du tablespace `badges.ibd` (mars 2023). L'ordre physique des
enregistrements donne l'ordre des identifiants d'epoque ; les positions 19 et 24
sont les deux badges supprimes, ce qui confirme la table de correspondance du TODO.

| # | id | Libelle d'epoque | Description d'epoque |
|---|---|---|---|
| 1 | 1 | Your first success | Find your first kikole |
| 2 | 2 | Halfway to the top | Find the kikole while getting 500 points or more |
| 3 | 3 | IT'S OVER 900 | Find the kikole while getting 900 points or more |
| 4 | 4 | Archaeology | Find a kikole born before 1970 |
| 5 | 5 | Three in a row | Find three kikole in a row (own submissions ignored) |
| 6 | 6 | A week in a row | Find seven kikole in a row (own submissions ignored) |
| 7 | 7 | Stay up late | Find a kikole before 2 AM |
| 8 | 8 | Saved by the bell | Find a kikole between 23PM and midnight |
| 9 | 9 | Caca, Café, Clope, Kikolé | Find a kikole between 5AM and 8AM |
| 10 | 10 | Over the top, part 1 | Find the kikole before everyone else |
| 11 | 11 | Over the top, part 2 | Find the kikole with more points than everyone else |
| 12 | 12 | Legend tier | Find 30 kikole in a row (own submissions ignored) |
| 13 | 13 | World war II | Find a kikole born before 1940 |
| 14 | 14 | Make it double | Score 1000 points twice in a row (own submissions ignored) |
| 15 | 15 | Do it yourself | Submit a kikole |
| 16 | 16 | Four Four two | Find enough kikoles to create a 442 formation + goalkeeper |
| 17 | 17 | Around the world | Find kikoles from 20 countries |
| 18 | 18 | Wooden spoon | Find a kikole without scoring points |
| 19 | **supprime** | Do you speak patois ? | Find the badge hidden in the site |
| 20 | 19 | We are kikolé | Submit 5 kikoles |
| 21 | 20 | Wikipedia screenshot | Find a kikole solely with club submissions prior (at least one) |
| 22 | 21 | Passport check | Find a kikole without any club submission prio |
| 23 | 22 | Everything not lost | Find a kikole without any correct submission prior (at least one incorrect) |
| 24 | **supprime** | The end? | Reach the "end" of the game |
| 25 | 23 | I'm feeling lucky | Find the kikolé just by submitting his name |
| 26 | 24 | Dedicated | Search 30 kikoles in a row (own submissions ignored) |
| 27 | 25 | Hell of a week | Scores 6666 points or more in a week (own submissions ignored) |
| 28 | 26 | The Breakfast Club | Find 7 kikoles in a row before 9AM (own submissions ignored) |
| 29 | 27 | Métro, boulot, kikolé, dodo | Find 7 kikoles in a row after 9PM (own submissions ignored) |
| 30 | 28 | OneMinuteChrono | Find the kikole and every information, without error and without the easy clue, in less than a minute, counting from the first information found. Kikole must have at least 6 clubs. |

## Descriptions françaises

Extraites de `badge_translations.ibd`. **L'alignement avec la liste ci-dessus n'est pas
mécanique** : la table contient des traductions orphelines de badges supprimés avant la
sauvegarde. On y lit encore les trois badges liés aux *challenges*, fonctionnalité
abandonnée, et quatre « Trouvez le joueur secret » qui ressemblent à des paliers masqués.

```
Trouvez votre premier Kikolé3
Trouvez votre premier kikolé en 500 points ou plus3
Trouvez votre premier kikolé en 900 points ou plus!
Trouvez un kikolé né avant 19706
LTrouvez 3 kikolés à la suite (soumissions ignorées)6
[Trouvez 7 kikolés à la suite (soumissions ignorées)*
jTrouvez un kikolé avant 2 heures du matin#
yTrouvez un kikolé après 23 heures/
Trouvez un kikolé entre 5 et 8 heures du matin
Trouvez le kikolé le premier:
Trouvez le kikolé avec plus de points que vos adversaires
Trouvez le joueur secret
Trouvez le joueur secret7
Trouvez 30 kikolés à la suite (soumissions ignorées)!
Trouvez un kikolé né avant 1940@
Obtenez 1000 points deux fois d'affilée (soumissions ignorées)
Proposez un kikoléQ
Trouvez suffisamment de kikolés pour former une composition 442 + gardien de but+
Trouvez des kikolés de 20 pays différents)
Trouvez un kikolé sans marquer de points
Participez à un challenge"
KInitiez et complétez 5 challenges=
ZParticipez à un challenge incluant 80% de vos points ou plus
iTrouvez le joueur secret
xTrouvez le joueur secret$
Trouvez le badge caché dans le site
Proposez 5 kikolésM
Trouvez un kikolé en ne soumettant que des clubs au préalable (au moins un)8
Trouvez un kikolé sans soumettre de clubs au préalablea
Trouvez un kikolé en ne soumettant que des propositions incorrectes au préalable (au moins une)
Atteignez la "fin" du jeu.
Trouvez le kikolé juste en soumettant son nom8
Cherchez 30 kikolés à la suite (soumissions ignorées)>
Faites 6666 points ou plus sur 7 jours (soumissions ignorées)E
Trouvez 7 kikolés à la suite avant 9 heures (soumissions ignorées)G
Trouvez 7 kikolés à la suite après 21 heures (soumissions ignorées)
Trouvez un kikolé avec toutes ses informations, sans erreur et sans l'indice facile, en moins d'une minute.
Le compte à rebours commence à partir de la première information trouvée.
Le kikolé doit avoir une carrière de plus de 5 clubs.
```

## Un écart entre le code et les descriptions publiées

Le badge **OneMinuteChrono** est décrit en anglais par « Kikole must have at least 6 clubs »
et en français par « une carrière de plus de 5 clubs ». Le code teste
`if (p.Clubs.Count < 5) return false;` — donc **éligible dès 5 clubs** — alors que son
propre commentaire dit « More than 5 clubs ».

Les deux descriptions d'époque concordent entre elles et contredisent l'implémentation.
L'écart existe toujours dans `BadgeService.cs:143`.
