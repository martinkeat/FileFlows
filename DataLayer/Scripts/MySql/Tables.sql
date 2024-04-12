CREATE TABLE FileFlows
(
    Version       VARCHAR(36)        NOT NULL
);

CREATE TABLE DbObject
(
    Uid             VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
    Name            VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    Type            VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
    DateCreated     datetime           default           now(),
    DateModified    datetime           default           now(),
    Data            MEDIUMTEXT         COLLATE utf8_unicode_ci      NOT NULL
);
ALTER TABLE DbObject ADD INDEX (Type);
ALTER TABLE DbObject ADD INDEX (Name);
ALTER TABLE DbObject ADD FULLTEXT NameIndex(Name);

CREATE TABLE DbLogMessage
(
    ClientUid       VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL,
    LogDate         datetime           default           now(),
    Type            int                NOT NULL,
    Message         TEXT               COLLATE utf8_unicode_ci      NOT NULL
);
ALTER TABLE DbLogMessage ADD INDEX (ClientUid);
ALTER TABLE DbLogMessage ADD INDEX (LogDate);

CREATE TABLE DbStatistic
(
    Name            varchar(255)       COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
    Type            int                NOT NULL,
    Data            TEXT               COLLATE utf8_unicode_ci      NOT NULL
);


CREATE TABLE RevisionedObject
(
    Uid             VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
    RevisionUid     VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL,
    RevisionName    VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    RevisionType    VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
    RevisionDate    datetime           default           now(),
    RevisionCreated datetime           default           now(),
    RevisionData    MEDIUMTEXT         COLLATE utf8_unicode_ci      NOT NULL
);

CREATE TABLE LibraryFile
(
    -- common fields from DbObject
    Uid                 VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
    Name                VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    DateCreated         datetime           default           now()      NOT NULL,
    DateModified        datetime           default           now()      NOT NULL,
    
    -- properties
    RelativePath        VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    Status              int                NOT NULL,
    ProcessingOrder     int                NOT NULL,
    Fingerprint         VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
    FinalFingerprint    VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL        DEFAULT(''),
    IsDirectory         boolean            not null,
    Flags               int                not null                     DEFAULT(0),
    
    -- size
    OriginalSize        bigint             NOT NULL,
    FinalSize           bigint             NOT NULL,
    
    -- dates 
    CreationTime        datetime           default           now()      NOT NULL,
    LastWriteTime       datetime           default           now()      NOT NULL,
    HoldUntil           datetime           default           '1970-01-01 00:00:01'      NOT NULL,
    ProcessingStarted   datetime           default           now()      NOT NULL,
    ProcessingEnded     datetime           default           now()      NOT NULL,
    
    -- references
    LibraryUid          varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    LibraryName         VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL,
    FlowUid             varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    FlowName            VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL,
    DuplicateUid        varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    DuplicateName       VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    NodeUid             varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    NodeName            VARCHAR(100)       COLLATE utf8_unicode_ci      NOT NULL,
    WorkerUid           varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,
    ProcessOnNodeUid    varchar(36)        COLLATE utf8_unicode_ci      NOT NULL,

    -- output
    OutputPath          VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
    FailureReason       VARCHAR(512)       COLLATE utf8_unicode_ci      NOT NULL,
    NoLongerExistsAfterProcessing          boolean                      not null,

    -- json data
    OriginalMetadata    TEXT               COLLATE utf8_unicode_ci      NOT NULL,
    FinalMetadata       TEXT               COLLATE utf8_unicode_ci      NOT NULL,
    ExecutedNodes       TEXT               COLLATE utf8_unicode_ci      NOT NULL
);

ALTER TABLE LibraryFile ADD INDEX (Status);
ALTER TABLE LibraryFile ADD INDEX (DateModified);
-- index to make library file status/skybox faster
ALTER TABLE LibraryFile ADD INDEX (Status, HoldUntil, LibraryUid);



CREATE TABLE AuditLog
(
    OperatorUid     VARCHAR(36)        NOT NULL,
    OperatorName    VARCHAR(255)       NOT NULL,
    OperatorType    INT                NOT NULL,
    IPAddress       VARCHAR(50)        NOT NULL,
    LogDate         datetime,
    Action          INT                NOT NULL,
    ObjectType      VARCHAR(255)       NOT NULL,
    ObjectUid       VARCHAR(36)        NOT NULL,
    RevisionUid     VARCHAR(36)        NOT NULL,
    Parameters      TEXT               NOT NULL,
    Changes         TEXT               NOT NULL
);

ALTER TABLE AuditLog ADD INDEX (OperatorUid);
ALTER TABLE AuditLog ADD INDEX (LogDate);
