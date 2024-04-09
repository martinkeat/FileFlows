CREATE TABLE "FileFlows"
(
    "Version"       VARCHAR(36)        NOT NULL
);

CREATE TABLE "DbObject"
(
    "Uid"             uuid               NOT NULL          PRIMARY KEY,
    "Name"            VARCHAR(1024)      NOT NULL,
    "Type"            VARCHAR(255)       NOT NULL,
    "DateCreated"     TIMESTAMP           DEFAULT CURRENT_TIMESTAMP,
    "DateModified"    TIMESTAMP           DEFAULT CURRENT_TIMESTAMP,
    "Data"            TEXT               NOT NULL
);
CREATE INDEX ON "DbObject" ("Type");
CREATE INDEX ON "DbObject" ("Name");
CREATE INDEX NameIndex ON "DbObject" ("Name");

CREATE TABLE "DbLogMessage"
(
    "ClientUid"       VARCHAR(36)        NOT NULL,
    "LogDate"         TIMESTAMP          DEFAULT CURRENT_TIMESTAMP,
    "Type"            INT                NOT NULL,
    "Message"         TEXT               NOT NULL
);
CREATE INDEX ON "DbLogMessage" ("ClientUid");
CREATE INDEX ON "DbLogMessage" ("LogDate");

CREATE TABLE "DbStatistic"
(
    "Name"            VARCHAR(255)       NOT NULL          PRIMARY KEY,
    "Type"            int                NOT NULL,
    "Data"            TEXT               NOT NULL
);


CREATE TABLE "RevisionedObject"
(
    "Uid"             uuid               NOT NULL          PRIMARY KEY,
    "RevisionUid"     uuid               NOT NULL,
    "RevisionName"    VARCHAR(1024)      NOT NULL,
    "RevisionType"    VARCHAR(255)       NOT NULL,
    "RevisionDate"    TIMESTAMP           DEFAULT CURRENT_TIMESTAMP,
    "RevisionCreated" TIMESTAMP           DEFAULT CURRENT_TIMESTAMP,
    "RevisionData"    TEXT               NOT NULL
);

CREATE TABLE "LibraryFile"
(
    -- common fields from DbObject
    "Uid"                 uuid               NOT NULL          PRIMARY KEY,
    "Name"                VARCHAR(1024)      NOT NULL,
    "DateCreated"         TIMESTAMP           DEFAULT CURRENT_TIMESTAMP      NOT NULL,
    "DateModified"        TIMESTAMP           DEFAULT CURRENT_TIMESTAMP      NOT NULL,

    -- properties
    "RelativePath"        VARCHAR(1024)      NOT NULL,
    "Status"              INT                NOT NULL,
    "ProcessingOrder"     INT                NOT NULL,
    "Fingerprint"         VARCHAR(255)       NOT NULL,
    "FinalFingerprint"    VARCHAR(255)       NOT NULL        DEFAULT '',
    "IsDirectory"         BOOLEAN            NOT NULL,
    "Flags"               INT                NOT NULL                     DEFAULT 0,

    -- size
    "OriginalSize"        BIGINT             NOT NULL,
    "FinalSize"           BIGINT             NOT NULL,

    -- dates 
    "CreationTime"        TIMESTAMP           DEFAULT CURRENT_TIMESTAMP      NOT NULL,
    "LastWriteTime"       TIMESTAMP           DEFAULT CURRENT_TIMESTAMP      NOT NULL,
    "HoldUntil"           TIMESTAMP           DEFAULT TIMESTAMP '1970-01-01 00:00:01'      NOT NULL,
    "ProcessingStarted"   TIMESTAMP           DEFAULT CURRENT_TIMESTAMP      NOT NULL,
    "ProcessingEnded"     TIMESTAMP           DEFAULT CURRENT_TIMESTAMP      NOT NULL,

    -- references
    "LibraryUid"          VARCHAR(36)        NOT NULL,
    "LibraryName"         VARCHAR(100)       NOT NULL,
    "FlowUid"             VARCHAR(36)        NOT NULL,
    "FlowName"            VARCHAR(100)       NOT NULL,
    "DuplicateUid"        VARCHAR(36)        NOT NULL,
    "DuplicateName"       VARCHAR(1024)      NOT NULL,
    "NodeUid"             VARCHAR(36)        NOT NULL,
    "NodeName"            VARCHAR(100)       NOT NULL,
    "WorkerUid"           VARCHAR(36)        NOT NULL,
    "ProcessOnNodeUid"    VARCHAR(36)        NOT NULL,

    -- output
    "OutputPath"          VARCHAR(1024)      NOT NULL,
    "FailureReason"       VARCHAR(512)       NOT NULL,
    "NoLongerExistsAfterProcessing"          BOOLEAN                      NOT NULL,

    -- json data
    "OriginalMetadata"    TEXT                NOT NULL,
    "FinalMetadata"       TEXT                NOT NULL,
    "ExecutedNodes"       TEXT                NOT NULL
);

CREATE INDEX ON "LibraryFile" ("Status");
CREATE INDEX ON "LibraryFile" ("DateModified");
-- index to make library file status/skybox faster
CREATE INDEX ON "LibraryFile" ("Status", "HoldUntil", "LibraryUid");




CREATE TABLE "AuditLog"
(
    "OperatorUid"     VARCHAR(36)        NOT NULL,
    "OperatorName"    VARCHAR(255)       NOT NULL,
    "OperatorType"    INT                NOT NULL,
    "IPAddress"       VARCHAR(50)        NOT NULL,
    "LogDate"         TIMESTAMP          DEFAULT CURRENT_TIMESTAMP,    
    "Action"          INT                NOT NULL,
    "ObjectType"      VARCHAR(255)       NOT NULL,
    "ObjectUid"       VARCHAR(36)        NOT NULL,
    "Parameters"      TEXT               NOT NULL,
    "RevisionUid"     VARCHAR(36)        NOT NULL
);
CREATE INDEX ON "AuditLog" ("OperatorUid");
CREATE INDEX ON "AuditLog" ("LogDate");