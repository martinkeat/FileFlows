CREATE TABLE DbObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name            VARCHAR(1024)      NOT NULL,
    Type            VARCHAR(255)       NOT NULL,
    DateCreated     datetime           NOT NULL,
    DateModified    datetime           NOT NULL,
    Data            TEXT               NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_DbObject_Type ON DbObject (Type);
CREATE INDEX IF NOT EXISTS idx_DbObject_Name ON DbObject (Name);

CREATE TABLE DbStatistic
(
    Name            varchar(255)       NOT NULL          PRIMARY KEY,
    Data            TEXT               NOT NULL
);


CREATE TABLE RevisionedObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    RevisionUid     VARCHAR(36)        NOT NULL,
    RevisionName    VARCHAR(1024)      NOT NULL,
    RevisionType    VARCHAR(255)       NOT NULL,
    RevisionDate    datetime           NOT NULL,
    RevisionCreated datetime           NOT NULL,
    RevisionData    TEXT               NOT NULL
);

CREATE TABLE LibraryFile
(
    -- common fields from DbObject
    Uid                 VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name                VARCHAR(1024)      NOT NULL          UNIQUE,
    DateCreated         datetime           NOT NULL,
    DateModified        datetime           NOT NULL,
    
    -- properties
    RelativePath        VARCHAR(1024)      NOT NULL,
    Status              int                NOT NULL,
    ProcessingOrder     int                NOT NULL,
    Fingerprint         VARCHAR(255)       NOT NULL,
    FinalFingerprint    VARCHAR(255)       NOT NULL         DEFAULT(''),
    IsDirectory         boolean            not null,
    Flags               int                not null         DEFAULT(0),
    
    -- size
    OriginalSize        bigint             NOT NULL,
    FinalSize           bigint             NOT NULL,
    
    -- dates 
    CreationTime        datetime           NOT NULL,
    LastWriteTime       datetime           NOT NULL,
    HoldUntil           datetime           default           '1970-01-01 00:00:01',
    ProcessingStarted   datetime           NOT NULL,
    ProcessingEnded     datetime           NOT NULL,
    
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
    NoLongerExistsAfterProcessing          boolean                      not null,

    -- json data
    OriginalMetadata    TEXT               NOT NULL,
    FinalMetadata       TEXT               NOT NULL,
    ExecutedNodes       TEXT               NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_LibraryFile_Status ON LibraryFile (Status);
CREATE INDEX IF NOT EXISTS idx_LibraryFile_DateModified ON LibraryFile (DateModified);
-- index to make library file status/skybox faster
CREATE INDEX IF NOT EXISTS idx_LibraryFile_StatusHoldLibrary ON LibraryFile (Status, HoldUntil, LibraryUid);