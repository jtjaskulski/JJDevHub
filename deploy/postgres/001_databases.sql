\getenv identity_write_password IDENTITY_WRITE_PASSWORD
SELECT (length(:'identity_write_password') > 0)::int AS identity_write_password_set \gset
\if :identity_write_password_set
\else
\echo brak IDENTITY_WRITE_PASSWORD
\quit 1
\endif
CREATE ROLE identity_write_owner WITH LOGIN PASSWORD :'identity_write_password';
CREATE DATABASE identity_write OWNER identity_write_owner;
REVOKE ALL ON DATABASE identity_write FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE identity_write TO identity_write_owner;

\getenv identity_read_password IDENTITY_READ_PASSWORD
SELECT (length(:'identity_read_password') > 0)::int AS identity_read_password_set \gset
\if :identity_read_password_set
\else
\echo brak IDENTITY_READ_PASSWORD
\quit 1
\endif
CREATE ROLE identity_read_owner WITH LOGIN PASSWORD :'identity_read_password';
CREATE DATABASE identity_read OWNER identity_read_owner;
REVOKE ALL ON DATABASE identity_read FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE identity_read TO identity_read_owner;

\getenv news_write_password NEWS_WRITE_PASSWORD
SELECT (length(:'news_write_password') > 0)::int AS news_write_password_set \gset
\if :news_write_password_set
\else
\echo brak NEWS_WRITE_PASSWORD
\quit 1
\endif
CREATE ROLE news_write_owner WITH LOGIN PASSWORD :'news_write_password';
CREATE DATABASE news_write OWNER news_write_owner;
REVOKE ALL ON DATABASE news_write FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE news_write TO news_write_owner;

\getenv news_read_password NEWS_READ_PASSWORD
SELECT (length(:'news_read_password') > 0)::int AS news_read_password_set \gset
\if :news_read_password_set
\else
\echo brak NEWS_READ_PASSWORD
\quit 1
\endif
CREATE ROLE news_read_owner WITH LOGIN PASSWORD :'news_read_password';
CREATE DATABASE news_read OWNER news_read_owner;
REVOKE ALL ON DATABASE news_read FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE news_read TO news_read_owner;

\getenv learning_write_password LEARNING_WRITE_PASSWORD
SELECT (length(:'learning_write_password') > 0)::int AS learning_write_password_set \gset
\if :learning_write_password_set
\else
\echo brak LEARNING_WRITE_PASSWORD
\quit 1
\endif
CREATE ROLE learning_write_owner WITH LOGIN PASSWORD :'learning_write_password';
CREATE DATABASE learning_write OWNER learning_write_owner;
REVOKE ALL ON DATABASE learning_write FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE learning_write TO learning_write_owner;

\getenv learning_read_password LEARNING_READ_PASSWORD
SELECT (length(:'learning_read_password') > 0)::int AS learning_read_password_set \gset
\if :learning_read_password_set
\else
\echo brak LEARNING_READ_PASSWORD
\quit 1
\endif
CREATE ROLE learning_read_owner WITH LOGIN PASSWORD :'learning_read_password';
CREATE DATABASE learning_read OWNER learning_read_owner;
REVOKE ALL ON DATABASE learning_read FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE learning_read TO learning_read_owner;

\getenv cv_write_password CV_WRITE_PASSWORD
SELECT (length(:'cv_write_password') > 0)::int AS cv_write_password_set \gset
\if :cv_write_password_set
\else
\echo brak CV_WRITE_PASSWORD
\quit 1
\endif
CREATE ROLE cv_write_owner WITH LOGIN PASSWORD :'cv_write_password';
CREATE DATABASE cv_write OWNER cv_write_owner;
REVOKE ALL ON DATABASE cv_write FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE cv_write TO cv_write_owner;

\getenv cv_read_password CV_READ_PASSWORD
SELECT (length(:'cv_read_password') > 0)::int AS cv_read_password_set \gset
\if :cv_read_password_set
\else
\echo brak CV_READ_PASSWORD
\quit 1
\endif
CREATE ROLE cv_read_owner WITH LOGIN PASSWORD :'cv_read_password';
CREATE DATABASE cv_read OWNER cv_read_owner;
REVOKE ALL ON DATABASE cv_read FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE cv_read TO cv_read_owner;

\getenv contact_write_password CONTACT_WRITE_PASSWORD
SELECT (length(:'contact_write_password') > 0)::int AS contact_write_password_set \gset
\if :contact_write_password_set
\else
\echo brak CONTACT_WRITE_PASSWORD
\quit 1
\endif
CREATE ROLE contact_write_owner WITH LOGIN PASSWORD :'contact_write_password';
CREATE DATABASE contact_write OWNER contact_write_owner;
REVOKE ALL ON DATABASE contact_write FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE contact_write TO contact_write_owner;

\getenv contact_read_password CONTACT_READ_PASSWORD
SELECT (length(:'contact_read_password') > 0)::int AS contact_read_password_set \gset
\if :contact_read_password_set
\else
\echo brak CONTACT_READ_PASSWORD
\quit 1
\endif
CREATE ROLE contact_read_owner WITH LOGIN PASSWORD :'contact_read_password';
CREATE DATABASE contact_read OWNER contact_read_owner;
REVOKE ALL ON DATABASE contact_read FROM PUBLIC;
GRANT ALL PRIVILEGES ON DATABASE contact_read TO contact_read_owner;
