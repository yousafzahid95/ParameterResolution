grammar PathExpression;

// Parser Rules for Path Expressions with Semantic Actions
pathExpression
    : segment ('.' segment)* EOF
    ;

segment
    : IDENTIFIER arrayAccess?
    ;

arrayAccess
    : '[' INTEGER ']'
    ;

// Semantic Parameter Extraction Rules
// These define the known patterns for extracting ProjectId, WorkAreaId, EntityId
parameterExtraction
    : projectIdPattern | workAreaIdPattern | entityIdPattern
    ;

// ProjectId patterns - matches common structures
projectIdPattern
    : 'InfoRequest' '.' 'ProjectId'                          # InfoRequestProjectId
    | 'LemEvent' '.' 'ProjectId'                             # LemEventProjectId
    | 'WorkplanTask' '.' 'ProjectId'                         # WorkplanTaskProjectId
    | 'ActionItemClosedEvent' '.' 'ProjectId'                # ActionItemProjectId
    | 'ProjectId'                                            # DirectProjectId
    ;

// WorkAreaId patterns - handles case variations
workAreaIdPattern
    : 'InfoRequest' '.' 'WorkareaId'                         # InfoRequestWorkAreaId
    | 'InfoRequest' '.' 'WorkAreaId'                         # InfoRequestWorkAreaIdAlt
    | 'LemEvent' '.' 'WorkareaId'                            # LemEventWorkAreaId
    | 'LemEvent' '.' 'WorkAreaId'                            # LemEventWorkAreaIdAlt
    | 'WorkplanTask' '.' 'WorkAreaId'                        # WorkplanTaskWorkAreaId
    | 'WorkplanTask' '.' 'WorkareaId'                        # WorkplanTaskWorkAreaIdAlt
    | 'ActionItemClosedEvent' '.' 'WorkAreaId'               # ActionItemWorkAreaId
    | 'WorkAreaId'                                           # DirectWorkAreaId
    | 'WorkareaId'                                           # DirectWorkAreaIdAlt
   ;

// EntityId patterns - includes array access patterns
entityIdPattern
    : 'InfoRequest' '.' 'EntityIds' '[' INTEGER ']'          # InfoRequestEntityIds
    | 'LemEvent' '.' 'EntityId'                              # LemEventEntityId
    | 'WorkplanTask' '.' 'Entities' '[' INTEGER ']' '.' 'WorkAreaEntityId'  # WorkplanTaskEntities
    | 'WorkplanTask' '.' 'EntityIds' '[' INTEGER ']'         # WorkplanTaskEntityIds
    | 'ActionItemClosedEvent' '.' 'EntityId'                 # ActionItemEntityId
    | 'Id'                                                   # DirectId
    | 'EntityId'                                             # DirectEntityId
    ;

// Lexer Rules
IDENTIFIER
    : [a-zA-Z_][a-zA-Z0-9_]*
    ;

INTEGER
    : [0-9]+
    ;

DOT
    : '.'
    ;

LBRACKET
    : '['
    ;

RBRACKET
    : ']'
    ;

WS
    : [ \t\r\n]+ -> skip
    ;
