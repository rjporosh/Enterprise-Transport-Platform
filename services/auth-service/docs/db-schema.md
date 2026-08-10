# Database Schema

## Tables

### users
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| email | VARCHAR(256) | UNIQUE, NOT NULL |
| password_hash | VARCHAR(512) | NOT NULL |
| first_name | VARCHAR(100) | NOT NULL |
| last_name | VARCHAR(100) | NOT NULL |
| phone_number | VARCHAR(30) | NULL |
| status | VARCHAR(30) | NOT NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |
| updated_at_utc | TIMESTAMPTZ | NOT NULL |

### roles
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| name | VARCHAR(50) | UNIQUE, NOT NULL |
| description | VARCHAR(200) | NULL |
| is_active | BOOLEAN | NOT NULL |

### user_roles
| Column | Type | Constraints |
|--------|------|-------------|
| user_id | UUID | PK,FK -> users.id |
| role_id | UUID | PK,FK -> roles.id |
| assigned_at_utc | TIMESTAMPTZ | NOT NULL |

### permissions
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| name | VARCHAR(100) | UNIQUE, NOT NULL |
| description | VARCHAR(500) | NULL |
| module | VARCHAR(100) | NOT NULL |
| is_active | BOOLEAN | NOT NULL |

### modules
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| name | VARCHAR(100) | UNIQUE, NOT NULL |
| description | VARCHAR(500) | NULL |
| is_active | BOOLEAN | NOT NULL |

### policies
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| name | VARCHAR(100) | UNIQUE, NOT NULL |
| description | VARCHAR(500) | NULL |
| is_active | BOOLEAN | NOT NULL |

### claims
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| type | VARCHAR(200) | NOT NULL |
| value | VARCHAR(500) | NOT NULL |
| user_id | UUID | FK -> users.id, NULL |
| role_id | UUID | FK -> roles.id, NULL |
| policy_id | UUID | FK -> policies.id, NULL |

### otp_records
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | FK -> users.id, NOT NULL |
| code_hash | VARCHAR(128) | NOT NULL |
| channel | VARCHAR(20) | NOT NULL |
| destination | VARCHAR(256) | NOT NULL |
| expires_at_utc | TIMESTAMPTZ | NOT NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |
| verified_at_utc | TIMESTAMPTZ | NULL |
| attempt_count | INT | NOT NULL |
| resend_count | INT | NOT NULL |
| is_used | BOOLEAN | NOT NULL |
| ip_address | VARCHAR(45) | NULL |

### security_questions
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| question_text | VARCHAR(500) | NOT NULL |
| is_active | BOOLEAN | NOT NULL |

### security_answers
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | FK -> users.id, NOT NULL |
| security_question_id | UUID | FK -> security_questions.id, NOT NULL |
| answer_hash | VARCHAR(256) | NOT NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |
| updated_at_utc | TIMESTAMPTZ | NOT NULL |

### user_security_questions
| Column | Type | Constraints |
|--------|------|-------------|
| user_id | UUID | PK,FK -> users.id |
| security_question_id | UUID | PK,FK -> security_questions.id |
| configured_at_utc | TIMESTAMPTZ | NOT NULL |

### password_histories
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | FK -> users.id, NOT NULL |
| password_hash | VARCHAR(512) | NOT NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |

### password_reset_tokens
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | FK -> users.id, NOT NULL |
| token_hash | VARCHAR(128) | UNIQUE, NOT NULL |
| expires_at_utc | TIMESTAMPTZ | NOT NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |
| used_at_utc | TIMESTAMPTZ | NULL |
| created_by_ip | VARCHAR(45) | NULL |

### user_sessions
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | FK -> users.id, NOT NULL |
| session_id | VARCHAR(128) | UNIQUE, NOT NULL |
| ip_address | VARCHAR(45) | NULL |
| user_agent | VARCHAR(512) | NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |
| expires_at_utc | TIMESTAMPTZ | NULL |
| last_activity_at_utc | TIMESTAMPTZ | NOT NULL |
| is_revoked | BOOLEAN | NOT NULL |
| revoked_at_utc | TIMESTAMPTZ | NULL |

### user_claims
| Column | Type | Constraints |
|--------|------|-------------|
| user_id | UUID | PK,FK -> users.id |
| type | VARCHAR(200) | PK |
| value | VARCHAR(500) | PK |
| created_at_utc | TIMESTAMPTZ | NOT NULL |

### role_permissions
| Column | Type | Constraints |
|--------|------|-------------|
| role_id | UUID | PK,FK -> roles.id |
| permission_id | UUID | PK,FK -> permissions.id |
| assigned_at_utc | TIMESTAMPTZ | NOT NULL |

### module_permissions
| Column | Type | Constraints |
|--------|------|-------------|
| module_id | UUID | PK,FK -> modules.id |
| permission_id | UUID | PK,FK -> permissions.id |
| assigned_at_utc | TIMESTAMPTZ | NOT NULL |

### refresh_tokens
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | FK -> users.id, NOT NULL |
| token_hash | VARCHAR(128) | UNIQUE, NOT NULL |
| expires_at_utc | TIMESTAMPTZ | NOT NULL |
| created_at_utc | TIMESTAMPTZ | NOT NULL |
| revoked_at_utc | TIMESTAMPTZ | NULL |
| replaced_by_token_hash | VARCHAR(128) | NULL |
| ip_address | VARCHAR(45) | NULL |
| user_agent | VARCHAR(512) | NULL |

### audit_logs
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| user_id | UUID | NULL |
| email | VARCHAR(256) | NULL |
| action | VARCHAR(40) | NOT NULL |
| success | BOOLEAN | NOT NULL |
| ip_address | VARCHAR(45) | NULL |
| user_agent | VARCHAR(512) | NULL |
| details | VARCHAR(2000) | NULL |
| occurred_at_utc | TIMESTAMPTZ | NOT NULL |

### outbox_messages
| Column | Type | Constraints |
|--------|------|-------------|
| id | UUID | PK |
| event_type | VARCHAR(500) | NOT NULL |
| payload | JSONB/TEXT | NOT NULL |
| occurred_on_utc | TIMESTAMPTZ | NOT NULL |
| processed_on_utc | TIMESTAMPTZ | NULL |
| error | TEXT | NULL |
| retry_count | INT | NOT NULL |
