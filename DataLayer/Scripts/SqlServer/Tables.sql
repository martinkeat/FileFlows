CREATE TABLE DbObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name            VARCHAR(1024)      NOT NULL,
    Type            VARCHAR(255)       NOT NULL,
    DateCreated     datetime           default           getdate(),
    DateModified    datetime           default           getdate(),
    Data            NVARCHAR(MAX)      NOT NULL
);
CREATE INDEX ix_Type ON DbObject (Type);
CREATE INDEX ix_Name ON DbObject (Name);

CREATE TABLE DbLogMessage
(
    ClientUid       VARCHAR(36)        NOT NULL,
    LogDate         datetime           default           getdate(),
    Type            int                NOT NULL,
    Message         NVARCHAR(MAX)      NOT NULL
);
CREATE INDEX ix_ClientUid ON DbLogMessage (ClientUid);
CREATE INDEX ix_LogDate ON DbLogMessage (LogDate);

CREATE TABLE DbStatistic
(
    Name            varchar(255)       NOT NULL          PRIMARY KEY,
    Data            NVARCHAR(MAX)      NOT NULL
);


CREATE TABLE RevisionedObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    RevisionUid     VARCHAR(36)        NOT NULL,
    RevisionName    VARCHAR(1024)      NOT NULL,
    RevisionType    VARCHAR(255)       NOT NULL,
    RevisionDate    datetime           default           getdate(),
    RevisionCreated datetime           default           getdate(),
    RevisionData    NVARCHAR(MAX)      NOT NULL
);

CREATE TABLE LibraryFile
(
    -- common fields from DbObject
    Uid                 VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name                VARCHAR(1024)      NOT NULL          UNIQUE,
    DateCreated         datetime           default           getdate()      NOT NULL,
    DateModified        datetime           default           getdate()      NOT NULL,

    -- properties
    RelativePath        VARCHAR(1024)      NOT NULL,
    Status              int                NOT NULL,
    ProcessingOrder     int                NOT NULL,
    Fingerprint         VARCHAR(255)       NOT NULL,
    FinalFingerprint    VARCHAR(255)       NOT NULL        DEFAULT(''),
    IsDirectory         bit                not null,
    Flags               int                not null                     DEFAULT(0),

    -- size
    OriginalSize        bigint             NOT NULL,
    FinalSize           bigint             NOT NULL,

    -- dates 
    CreationTime        datetime           default           getdate()      NOT NULL,
    LastWriteTime       datetime           default           getdate()      NOT NULL,
    HoldUntil           datetime           default           '1970-01-01 00:00:01'      NOT NULL,
    ProcessingStarted   datetime           default           getdate()      NOT NULL,
    ProcessingEnded     datetime           default           getdate()      NOT NULL,

    -- references
    LibraryUid          varchar(36)        NOT NULL,
    LibraryName         VARCHAR(100)       NOT NULL,
    FlowUid             varchar(36)        NOT NULL,
    FlowName            VARCHAR(100)       NOT NULL,
    DuplicateUid        varchar(36)        NOT NULL,
    DuplicateName       VARCHAR(1024)      NOT NULL,
    NodeUid             varchar(36)        NOT NULL,
    NodeName            VARCHAR(100)       NOT NULL,
    WorkerUid           varchar(36)        NOT NULL,
    ProcessOnNodeUid    varchar(36)        NOT NULL,

    -- output
    OutputPath          VARCHAR(1024)      NOT NULL,
    FailureReason       VARCHAR(512)       NOT NULL,
    NoLongerExistsAfterProcessing          bit                      not null,

    -- json data
    OriginalMetadata    NVARCHAR(MAX)      NOT NULL,
    FinalMetadata       NVARCHAR(MAX)      NOT NULL,
    ExecutedNodes       NVARCHAR(MAX)      NOT NULL
);

CREATE INDEX ix_Status ON LibraryFile (Status);
CREATE INDEX ix_DateModified ON LibraryFile (DateModified);
CREATE INDEX ix_Status_HoldUntil_LibraryUid ON LibraryFile (Status, HoldUntil, LibraryUid);
