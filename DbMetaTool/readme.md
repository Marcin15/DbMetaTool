## Opis Aplikacji DbMetaTool

Aplikacja zosta³a stworzona z myœl¹ o szybkim i ³atwym operowaniu na bazie danych Firebird 5.0.
Korzystanie z aplikacji odbywa siê poprzez uruchomienie pliku uruchomieniowego `DbMetaTool.exe` w terminalu, który znajduje siê w katalogu wynikowym aplikacji.
Aplikacja oczekuje nastêpuj¹cych argumentów:

- `build-db` - tworzy now¹ bazê danych Firebird o nazwie "database.fdb" w wybranym przez u¿ytkownika katalogu, na podstawie wskazanych skryptów SQL. Dodatkowe parametry:
  - `--db-dir <œcie¿ka>` - katalog w którym zostanie utworzona baza danych
  - `--scripts-dir <œcie¿ka>` - katalog zawieraj¹cy skrypty SQL do utworzenia bazy danych
- `export-scripts` - eksportuje skrypty SQL z istniej¹cej bazy danych do plików w wybranym katalogu. Dodatkowe parametry:
  - `--connection-string <connStr>` - ci¹g po³¹czenia do istniej¹cej bazy danych Firebird
  - `--output-dir <œcie¿ka>` - katalog, do którego zostan¹ zapisane wyeksportowane skrypty SQL
- `update-db` - aktualizuje istniej¹c¹ bazê danych Firebird na podstawie skryptów SQL znajduj¹cych siê w wybranym katalogu. Dodatkowe parametry:
  - `--connection-string <connStr>` - ci¹g po³¹czenia do istniej¹cej bazy danych Firebird
  - `--scripts-dir <œcie¿ka>` - katalog zawieraj¹cy skrypty SQL do aktualizacji bazy danych

  Baza danych zak³ada siê z nastêpuj¹cymi parametrami po³¹czenia:
- `userId`: SYSDBA
- `password`: masterkey

W katalogu `Scripts` znajduj¹ siê przyk³adowe skrypty SQL, które mo¿na wykorzystaæ do tworzenia i aktualizacji bazy danych.