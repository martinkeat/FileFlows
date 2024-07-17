CREATE TABLE FileFlows
(
    Version       VARCHAR(36)        NOT NULL
);

CREATE TABLE DbObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name            NVARCHAR(1024)     NOT NULL,
    Type            VARCHAR(255)       NOT NULL,
    DateCreated     datetime           default           getdate(),
    DateModified    datetime           default           getdate(),
    Data            NVARCHAR(MAX)      NOT NULL
);
CREATE INDEX ix_Type ON DbObject (Type);
CREATE INDEX ix_Name ON DbObject (Name);


CREATE TABLE DbStatistic
(
    Name            varchar(255)       NOT NULL          PRIMARY KEY,
    Type            int                NOT NULL,
    Data            NVARCHAR(MAX)      NOT NULL
);


CREATE TABLE RevisionedObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    RevisionUid     VARCHAR(36)        NOT NULL,
    RevisionName    NVARCHAR(1024)     NOT NULL,
    RevisionType    VARCHAR(255)       NOT NULL,
    RevisionDate    datetime           default           getdate(),
    RevisionCreated datetime           default           getdate(),
    RevisionData    NVARCHAR(MAX)      NOT NULL
);

CREATE TABLE LibraryFile
(
    -- common fields from DbObject
    Uid                 VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name                NVARCHAR(1024)     NOT NULL,
    DateCreated         datetime           default           getdate()      NOT NULL,
    DateModified        datetime           default           getdate()      NOT NULL,

    -- properties
    RelativePath        NVARCHAR(1024)     NOT NULL,
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
    LibraryName         NVARCHAR(100)      NOT NULL,
    FlowUid             varchar(36)        NOT NULL,
    FlowName            NVARCHAR(100)      NOT NULL,
    DuplicateUid        varchar(36)        NOT NULL,
    DuplicateName       NVARCHAR(1024)     NOT NULL,
    NodeUid             varchar(36)        NOT NULL,
    NodeName            NVARCHAR(100)      NOT NULL,
    WorkerUid           varchar(36)        NOT NULL,
    ProcessOnNodeUid    varchar(36)        NOT NULL,

    -- output
    OutputPath          NVARCHAR(1024)     NOT NULL,
    FailureReason       NVARCHAR(512)      NOT NULL,
    NoLongerExistsAfterProcessing          bit                      not null,

    -- json data
    OriginalMetadata    NVARCHAR(MAX)      NOT NULL,
    FinalMetadata       NVARCHAR(MAX)      NOT NULL,
    ExecutedNodes       NVARCHAR(MAX)      NOT NULL
);

CREATE INDEX ix_Status ON LibraryFile (Status);
CREATE INDEX ix_DateModified ON LibraryFile (DateModified);
CREATE INDEX ix_Status_HoldUntil_LibraryUid ON LibraryFile (Status, HoldUntil, LibraryUid);


CREATE TABLE AuditLog
(
    OperatorUid     VARCHAR(36)        NOT NULL,
    OperatorName    NVARCHAR(255)      NOT NULL,
    OperatorType    INT                NOT NULL,
    IPAddress       VARCHAR(50)        NOT NULL,
    LogDate         datetime,
    Action          INT                NOT NULL,
    ObjectType      VARCHAR(255)       NOT NULL,
    ObjectUid       VARCHAR(36)        NOT NULL,
    RevisionUid     VARCHAR(36)        NOT NULL,
    Parameters      NVARCHAR(MAX)      NOT NULL,
    Changes         NVARCHAR(MAX)      NOT NULL
);

CREATE INDEX ix_AuditLog_OperatorUid ON AuditLog (OperatorUid);
CREATE INDEX ix_AuditLog_LogDate ON AuditLog (LogDate);