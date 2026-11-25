-- appTEA - esquema de base de datos (Supabase / Postgres)
-- Basado en docs/diagrama-bd.md. Pegar en el SQL Editor de Supabase, o aplicar con la CLI (supabase db push).
--
-- Convenciones:
--  * Los ids de sesion, metricas y detalles los genera la tablet (UUID) para que reintentar una subida no duplique.
--  * Los clientes NO escriben directo en las tablas de juego ni de vinculacion: usan las funciones
--    upload_game_session, create_link_code, redeem_link_code y revoke_assignment (y review_professional, solo administradores).
--  * Row Level Security activo en todas las tablas.

-- =====================================================================
-- Tablas
-- =====================================================================

create table public.users (
    id          uuid primary key references auth.users (id) on delete cascade,
    name        text not null default '',
    gmail       text not null unique,
    -- admin: quien revisa las verificaciones de profesionales. Nunca se asigna desde la app
    -- (handle_new_user siempre crea 'parent' y los clientes solo pueden actualizar su nombre): lo cambia el dueño del proyecto.
    role        text not null default 'parent' check (role in ('parent', 'professional', 'admin')),
    created_at  timestamptz not null default now()
);

create table public.profiles (
    id                uuid primary key default gen_random_uuid(),
    user_id           uuid not null references public.users (id) on delete cascade,
    dni               text,
    name              text not null,
    age               int check (age between 0 and 18),
    avatar            text,
    pet_name          text,
    pet_appearance    text,
    daily_time_limit  int check (daily_time_limit >= 0),   -- minutos
    status            text not null default 'active',
    created_at        timestamptz not null default now()
);
create index profiles_user_id_idx on public.profiles (user_id);

create table public.accessibility_config (
    id              uuid primary key default gen_random_uuid(),
    profile_id      uuid not null unique references public.profiles (id) on delete cascade,
    color_palette   text,
    sound_enabled   boolean not null default true,
    volume          int check (volume between 0 and 100)
);

create table public.professionals (
    id                    uuid primary key default gen_random_uuid(),
    user_id               uuid not null unique references public.users (id) on delete cascade,
    dni                   text,
    license_number        text,
    verification_status   text not null default 'pending'
                          check (verification_status in ('pending', 'verified', 'rejected', 'suspended')),
    verified_at           timestamptz
);

-- Guarda solo RESULTADOS de la verificacion: nunca imagenes ni datos biometricos.
create table public.identity_verifications (
    id                   uuid primary key default gen_random_uuid(),
    professional_id      uuid not null references public.professionals (id) on delete cascade,
    method               text check (method in ('biometrica', 'manual')),
    provider             text,                    -- ej. didit
    provider_reference_id text,
    consent_at           timestamptz,             -- consentimiento explicito para el uso de datos biometricos
    identity_result      text check (identity_result in ('aprobado', 'rechazado', 'dudoso')),
    registry_source      text,                    -- REFEPS
    registry_checked_at  timestamptz,
    registry_result      text check (registry_result in ('habilitado', 'no_encontrado', 'no_habilitado')),
    reviewed_by          uuid references public.users (id) on delete set null,   -- administrador que reviso (opcional)
    rejection_reason     text,
    dni_document_url     text,                    -- solo camino manual; se borra al resolver
    status               text not null default 'pending',
    verified_at          timestamptz,
    created_at           timestamptz not null default now()
);

create table public.specialties (
    id    uuid primary key default gen_random_uuid(),
    name  text not null unique
);

create table public.professional_specialties (
    id               uuid primary key default gen_random_uuid(),
    professional_id  uuid not null references public.professionals (id) on delete cascade,
    specialty_id     uuid not null references public.specialties (id),
    unique (professional_id, specialty_id)
);

-- Codigo de vinculacion padre -> profesional. Se guarda solo el hash.
create table public.profile_link_codes (
    id          uuid primary key default gen_random_uuid(),
    profile_id  uuid not null references public.profiles (id) on delete cascade,
    created_by  uuid not null references public.users (id) on delete cascade,
    code_hash   text not null unique,
    expires_at  timestamptz not null,
    used_at     timestamptz,
    used_by     uuid references public.professionals (id),
    created_at  timestamptz not null default now()
);
create index profile_link_codes_profile_idx on public.profile_link_codes (profile_id);

-- Intentos de canje, para limitar los fallidos. (Un intento con un codigo inexistente no se puede
-- anotar en profile_link_codes, por eso hace falta esta tabla.)
create table public.profile_link_attempts (
    id               uuid primary key default gen_random_uuid(),
    professional_id  uuid not null references public.professionals (id) on delete cascade,
    attempted_at     timestamptz not null default now(),
    success          boolean not null
);
create index profile_link_attempts_idx on public.profile_link_attempts (professional_id, attempted_at);

create table public.professional_assignments (
    id               uuid primary key default gen_random_uuid(),
    professional_id  uuid not null references public.professionals (id) on delete cascade,
    profile_id       uuid not null references public.profiles (id) on delete cascade,
    link_code_id     uuid references public.profile_link_codes (id),
    assigned_at      timestamptz not null default now(),
    status           text not null default 'active' check (status in ('active', 'revoked')),
    revoked_at       timestamptz,
    notes            text,
    check ((status = 'revoked') = (revoked_at is not null))
);
-- Un profesional tiene como maximo una vinculacion vigente por niño
create unique index professional_assignments_active_uq
    on public.professional_assignments (professional_id, profile_id) where status = 'active';
create index professional_assignments_profile_idx on public.professional_assignments (profile_id);

create table public.notifications (
    id             uuid primary key default gen_random_uuid(),
    user_id        uuid not null references public.users (id) on delete cascade,
    assignment_id  uuid references public.professional_assignments (id) on delete set null,
    type           text not null,
    message        text not null,
    is_read        boolean not null default false,
    created_at     timestamptz not null default now()
);
create index notifications_user_idx on public.notifications (user_id, created_at desc);

create table public.games (
    id              uuid primary key default gen_random_uuid(),
    name            text not null unique,
    description     text,
    category        text,
    max_levels      int,
    is_cooperative  boolean not null default false,
    is_active       boolean not null default true,
    created_at      timestamptz not null default now()
);

create table public.game_sessions (
    id            uuid primary key default gen_random_uuid(),   -- la tablet manda su propio UUID
    game_id       uuid not null references public.games (id),
    started_at    timestamptz not null,
    ended_at      timestamptz,
    duration_sec  int check (duration_sec >= 0),                -- tiempo ACTIVO (sin pausas)
    paused_sec    int not null default 0 check (paused_sec >= 0),
    completed     boolean not null default false,
    difficulty    text check (difficulty in ('facil', 'medio', 'dificil')),
    level         int
);
create index game_sessions_game_started_idx on public.game_sessions (game_id, started_at desc);

create table public.game_session_participants (
    id           uuid primary key default gen_random_uuid(),
    session_id   uuid not null references public.game_sessions (id) on delete cascade,
    profile_id   uuid not null references public.profiles (id) on delete cascade,
    player_slot  int not null default 1,
    unique (session_id, player_slot)
);
create index game_session_participants_profile_idx on public.game_session_participants (profile_id);

-- Las metricas se asocian al NIÑO PARTICIPANTE (no a la sesion): Identificar objetos mide por jugador.
create table public.metrics (
    id               uuid primary key default gen_random_uuid(),
    participant_id   uuid not null references public.game_session_participants (id) on delete cascade,
    correct_answers  int,          -- nullable: Escribir siguiendo la linea no tiene aciertos/errores
    wrong_answers    int,
    hints_used       int not null default 0,
    recorded_at      timestamptz not null default now()
);
create index metrics_participant_idx on public.metrics (participant_id);

create table public.game_metrics_detail (
    id              uuid primary key default gen_random_uuid(),
    participant_id  uuid not null references public.game_session_participants (id) on delete cascade,
    metric_key      text not null,
    metric_value    float not null,
    unique (participant_id, metric_key)
);

create table public.reports (
    id               uuid primary key default gen_random_uuid(),
    profile_id       uuid not null references public.profiles (id) on delete cascade,
    generated_by     uuid not null references public.professionals (id),
    report_type      text,
    date_from        date,
    date_to          date,
    total_sessions   int,
    total_time_sec   int,
    avg_correct_pct  float,
    observations     text,
    generated_at     timestamptz not null default now()
);
create index reports_profile_idx on public.reports (profile_id);

create table public.report_games (
    id         uuid primary key default gen_random_uuid(),
    report_id  uuid not null references public.reports (id) on delete cascade,
    game_id    uuid not null references public.games (id),
    unique (report_id, game_id)
);

-- =====================================================================
-- Alta automatica de la fila en public.users al registrarse en Supabase Auth
-- =====================================================================

create function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    insert into public.users (id, name, gmail)
    values (new.id, coalesce(new.raw_user_meta_data ->> 'name', ''), new.email);
    return new;
end;
$$;

create trigger on_auth_user_created
    after insert on auth.users
    for each row execute function public.handle_new_user();

-- =====================================================================
-- Funciones auxiliares de acceso (usadas por las politicas RLS)
-- =====================================================================

create function public.my_professional_id()
returns uuid
language sql stable security definer
set search_path = ''
as $$
    select p.id from public.professionals p
    where p.user_id = auth.uid() and p.verification_status = 'verified'
$$;

create function public.is_admin()
returns boolean
language sql stable security definer
set search_path = ''
as $$
    select exists (select 1 from public.users where id = auth.uid() and role = 'admin')
$$;

create function public.is_parent_of(p_profile uuid)
returns boolean
language sql stable security definer
set search_path = ''
as $$
    select exists (select 1 from public.profiles where id = p_profile and user_id = auth.uid())
$$;

-- Profesional verificado con una vinculacion vigente sobre ese niño
create function public.is_professional_of(p_profile uuid)
returns boolean
language sql stable security definer
set search_path = ''
as $$
    select exists (
        select 1
        from public.professional_assignments a
        where a.profile_id = p_profile
          and a.status = 'active'
          and a.professional_id = public.my_professional_id()
    )
$$;

create function public.can_view_profile(p_profile uuid)
returns boolean
language sql stable security definer
set search_path = ''
as $$
    select public.is_parent_of(p_profile) or public.is_professional_of(p_profile)
$$;

create function public.can_view_session(p_session uuid)
returns boolean
language sql stable security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.game_session_participants gsp
        where gsp.session_id = p_session and public.can_view_profile(gsp.profile_id)
    )
$$;

create function public.can_view_participant(p_participant uuid)
returns boolean
language sql stable security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.game_session_participants gsp
        where gsp.id = p_participant and public.can_view_profile(gsp.profile_id)
    )
$$;

-- =====================================================================
-- Row Level Security
-- =====================================================================

alter table public.users                    enable row level security;
alter table public.profiles                 enable row level security;
alter table public.accessibility_config     enable row level security;
alter table public.professionals            enable row level security;
alter table public.identity_verifications   enable row level security;
alter table public.specialties              enable row level security;
alter table public.professional_specialties enable row level security;
alter table public.profile_link_codes       enable row level security;
alter table public.profile_link_attempts    enable row level security;   -- sin politicas: solo las funciones
alter table public.professional_assignments enable row level security;
alter table public.notifications            enable row level security;
alter table public.games                    enable row level security;
alter table public.game_sessions            enable row level security;
alter table public.game_session_participants enable row level security;
alter table public.metrics                  enable row level security;
alter table public.game_metrics_detail      enable row level security;
alter table public.reports                  enable row level security;
alter table public.report_games             enable row level security;

-- users
create policy users_select_own on public.users for select to authenticated using (id = auth.uid());
create policy users_update_own on public.users for update to authenticated
    using (id = auth.uid()) with check (id = auth.uid());

-- profiles: el padre gestiona los suyos; el profesional vinculado solo los lee
create policy profiles_parent_all on public.profiles for all to authenticated
    using (user_id = auth.uid()) with check (user_id = auth.uid());
create policy profiles_professional_select on public.profiles for select to authenticated
    using (public.is_professional_of(id));

create policy accessibility_parent_all on public.accessibility_config for all to authenticated
    using (public.is_parent_of(profile_id)) with check (public.is_parent_of(profile_id));
create policy accessibility_professional_select on public.accessibility_config for select to authenticated
    using (public.is_professional_of(profile_id));

-- professionals: cada uno ve el suyo y puede registrarse, pero NUNCA se auto-verifica
create policy professionals_select_own on public.professionals for select to authenticated
    using (user_id = auth.uid());
create policy professionals_insert_own on public.professionals for insert to authenticated
    with check (user_id = auth.uid() and verification_status = 'pending' and verified_at is null);
-- (verification_status solo lo cambia un administrador, mediante review_professional; ningun otro cliente puede)

create policy identity_select_own on public.identity_verifications for select to authenticated
    using (professional_id in (select id from public.professionals where user_id = auth.uid()));
-- El profesional solo puede iniciar su verificacion (metodo y consentimiento): los resultados
-- (identidad, REFEPS, revision) los carga un administrador con la service_role key, nunca la app.
create policy identity_insert_own on public.identity_verifications for insert to authenticated
    with check (status = 'pending' and verified_at is null
                and identity_result is null and registry_checked_at is null and registry_result is null
                and reviewed_by is null and rejection_reason is null
                and professional_id in (select id from public.professionals where user_id = auth.uid()));

-- el administrador ve las solicitudes de verificacion (profesionales, sus datos de contacto y los resultados);
-- el estado lo cambia solo con review_professional
create policy users_select_admin on public.users for select to authenticated
    using (public.is_admin() and role = 'professional');
create policy professionals_select_admin on public.professionals for select to authenticated
    using (public.is_admin());
create policy identity_select_admin on public.identity_verifications for select to authenticated
    using (public.is_admin());

create policy specialties_select on public.specialties for select to authenticated using (true);

create policy prof_specialties_own on public.professional_specialties for all to authenticated
    using (professional_id in (select id from public.professionals where user_id = auth.uid()))
    with check (professional_id in (select id from public.professionals where user_id = auth.uid()));

-- codigos de vinculacion: el padre solo los lista (se crean con create_link_code); el hash no se expone
create policy link_codes_select_parent on public.profile_link_codes for select to authenticated
    using (public.is_parent_of(profile_id));

-- vinculaciones: las ven el padre y el profesional involucrado; se crean/revocan solo por funcion
create policy assignments_select_parent on public.professional_assignments for select to authenticated
    using (public.is_parent_of(profile_id));
create policy assignments_select_professional on public.professional_assignments for select to authenticated
    using (professional_id in (select id from public.professionals where user_id = auth.uid()));

create policy notifications_select_own on public.notifications for select to authenticated
    using (user_id = auth.uid());
create policy notifications_update_own on public.notifications for update to authenticated
    using (user_id = auth.uid()) with check (user_id = auth.uid());

create policy games_select on public.games for select to authenticated using (true);

-- juego y metricas: lectura para quien puede ver al niño; la escritura es solo por upload_game_session
create policy sessions_select on public.game_sessions for select to authenticated
    using (public.can_view_session(id));
create policy participants_select on public.game_session_participants for select to authenticated
    using (public.can_view_profile(profile_id));
create policy metrics_select on public.metrics for select to authenticated
    using (public.can_view_participant(participant_id));
create policy metrics_detail_select on public.game_metrics_detail for select to authenticated
    using (public.can_view_participant(participant_id));

-- reportes: los genera un profesional vinculado; los ve el padre y los profesionales vinculados
create policy reports_select on public.reports for select to authenticated
    using (public.can_view_profile(profile_id));
create policy reports_insert_professional on public.reports for insert to authenticated
    with check (generated_by = public.my_professional_id() and public.is_professional_of(profile_id));
create policy report_games_select on public.report_games for select to authenticated
    using (exists (select 1 from public.reports r where r.id = report_id and public.can_view_profile(r.profile_id)));
create policy report_games_insert on public.report_games for insert to authenticated
    with check (exists (select 1 from public.reports r
                        where r.id = report_id and r.generated_by = public.my_professional_id()));

-- Permisos a nivel de columna: el hash del codigo no se lee y las notificaciones solo permiten marcar "leida"
revoke select on public.profile_link_codes from authenticated;
grant  select (id, profile_id, created_by, expires_at, used_at, used_by, created_at)
    on public.profile_link_codes to authenticated;
revoke update on public.notifications from authenticated;
grant  update (is_read) on public.notifications to authenticated;
revoke update on public.users from authenticated;
grant  update (name) on public.users to authenticated;

-- =====================================================================
-- Vinculacion con profesionales
-- =====================================================================

-- El padre genera un codigo para un niño. Devuelve el codigo EN CLARO una sola vez; en la base queda el hash.
create function public.create_link_code(p_profile uuid, p_ttl_hours int default 24)
returns table (code text, valid_until timestamptz)
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_code  text;
    v_hash  text;
    v_exp   timestamptz;
begin
    if auth.uid() is null or not public.is_parent_of(p_profile) then
        raise exception 'not_allowed' using errcode = '42501';
    end if;
    if p_ttl_hours < 1 or p_ttl_hours > 168 then
        raise exception 'invalid_ttl';
    end if;

    -- un solo codigo vigente por niño: los anteriores sin usar se anulan
    update public.profile_link_codes
       set expires_at = now()
     where profile_id = p_profile and used_at is null and expires_at > now();

    v_exp := now() + make_interval(hours => p_ttl_hours);
    loop
        -- 10 caracteres hexadecimales (40 bits) a partir de gen_random_uuid(), que es criptograficamente aleatorio
        v_code := upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 10));
        v_hash := encode(sha256(convert_to(v_code, 'UTF8')), 'hex');
        begin
            insert into public.profile_link_codes (profile_id, created_by, code_hash, expires_at)
            values (p_profile, auth.uid(), v_hash, v_exp);
            exit;
        exception when unique_violation then
            null;   -- colision improbable: reintenta
        end;
    end loop;

    return query select substr(v_code, 1, 5) || '-' || substr(v_code, 6, 5), v_exp;
end;
$$;

-- El profesional canjea el codigo. Devuelve jsonb {ok, error, assignment_id}; no lanza excepciones por codigo
-- invalido para que el intento fallido quede registrado (una excepcion revertiria el registro).
create function public.redeem_link_code(p_code text)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
    c_max_failed constant int := 5;
    c_window     constant interval := interval '15 minutes';
    v_pro        uuid;
    v_failed     int;
    v_hash       text;
    v_code       public.profile_link_codes%rowtype;
    v_assign     uuid;
    v_pro_name   text;
begin
    v_pro := public.my_professional_id();
    if auth.uid() is null or v_pro is null then
        return jsonb_build_object('ok', false, 'error', 'not_verified_professional');
    end if;

    select count(*) into v_failed
      from public.profile_link_attempts
     where professional_id = v_pro and not success and attempted_at > now() - c_window;
    if v_failed >= c_max_failed then
        return jsonb_build_object('ok', false, 'error', 'too_many_attempts');
    end if;

    v_hash := encode(sha256(convert_to(upper(regexp_replace(coalesce(p_code, ''), '[^A-Za-z0-9]', '', 'g')), 'UTF8')), 'hex');

    select * into v_code
      from public.profile_link_codes
     where code_hash = v_hash and used_at is null and expires_at > now()
       for update;

    if not found then
        insert into public.profile_link_attempts (professional_id, success) values (v_pro, false);
        return jsonb_build_object('ok', false, 'error', 'invalid_or_expired_code');
    end if;

    update public.profile_link_codes set used_at = now(), used_by = v_pro where id = v_code.id;

    insert into public.professional_assignments (professional_id, profile_id, link_code_id)
    values (v_pro, v_code.profile_id, v_code.id)
    on conflict (professional_id, profile_id) where status = 'active' do nothing
    returning id into v_assign;

    if v_assign is null then   -- ya estaba vinculado
        select id into v_assign from public.professional_assignments
         where professional_id = v_pro and profile_id = v_code.profile_id and status = 'active';
    else
        select u.name into v_pro_name
          from public.professionals p join public.users u on u.id = p.user_id where p.id = v_pro;
        insert into public.notifications (user_id, assignment_id, type, message)
        values (v_code.created_by, v_assign, 'professional_linked',
                coalesce(nullif(v_pro_name, ''), 'Un profesional') || ' quedo vinculado al perfil.');
    end if;

    insert into public.profile_link_attempts (professional_id, success) values (v_pro, true);
    return jsonb_build_object('ok', true, 'assignment_id', v_assign);
end;
$$;

-- El padre revoca el acceso de un profesional
create function public.revoke_assignment(p_assignment uuid)
returns void
language plpgsql
security definer
set search_path = ''
as $$
begin
    update public.professional_assignments a
       set status = 'revoked', revoked_at = now()
     where a.id = p_assignment and a.status = 'active' and public.is_parent_of(a.profile_id);
    if not found then
        raise exception 'not_found_or_not_allowed' using errcode = '42501';
    end if;
end;
$$;

-- =====================================================================
-- Verificacion de profesionales (solo administradores)
-- =====================================================================
-- El administrador registra el resultado de la identidad (proveedor externo) y de la consulta manual a REFEPS,
-- y resuelve la solicitud. Para quedar 'verified' hacen falta las dos comprobaciones aprobadas.
-- Tambien sirve para rechazar o suspender (por ejemplo, en la revision periodica de la matricula).
create function public.review_professional(
    p_professional     uuid,
    p_status           text,
    p_identity_result  text,
    p_registry_result  text,
    p_reason           text default null)
returns void
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_row uuid;
begin
    if auth.uid() is null or not public.is_admin() then
        raise exception 'not_allowed' using errcode = '42501';
    end if;
    if p_status not in ('verified', 'rejected', 'suspended') then
        raise exception 'invalid_status';
    end if;
    if p_status = 'verified'
       and (p_identity_result is distinct from 'aprobado' or p_registry_result is distinct from 'habilitado') then
        raise exception 'verification_incomplete';
    end if;

    if not exists (select 1 from public.professionals where id = p_professional) then
        raise exception 'not_found';
    end if;

    select id into v_row from public.identity_verifications
     where professional_id = p_professional order by created_at desc limit 1;
    if v_row is null then
        insert into public.identity_verifications (professional_id) values (p_professional) returning id into v_row;
    end if;

    update public.identity_verifications
       set identity_result     = p_identity_result,
           registry_source     = case when p_registry_result is not null then 'REFEPS' end,
           registry_checked_at = case when p_registry_result is not null then now() end,
           registry_result     = p_registry_result,
           reviewed_by         = auth.uid(),
           rejection_reason    = case when p_status = 'rejected' then p_reason end,
           status              = p_status,
           verified_at         = case when p_status = 'verified' then now() end,
           dni_document_url    = null          -- camino manual: el documento se descarta al resolver
     where id = v_row;

    update public.professionals
       set verification_status = p_status,
           verified_at         = case when p_status = 'verified' then now() end
     where id = p_professional;
end;
$$;

-- =====================================================================
-- Subida de una sesion de juego (idempotente: reintentar no duplica)
-- =====================================================================
-- payload = {
--   "profile_id": "...",
--   "session":  { id, game_id, started_at, ended_at, duration_sec, paused_sec, completed, difficulty, level },
--   "metrics":  { id, correct_answers, wrong_answers, hints_used, recorded_at }   (opcional; se guarda por participante)
--   "details":  [ { id, metric_key, metric_value }, ... ]                                          (opcional)
-- }
create function public.upload_game_session(payload jsonb)
returns void
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_profile  uuid := (payload ->> 'profile_id')::uuid;
    s          jsonb := payload -> 'session';
    m          jsonb := payload -> 'metrics';
    d          jsonb;
    v_session  uuid;
    v_participant uuid;
begin
    if auth.uid() is null or v_profile is null or not public.is_parent_of(v_profile) then
        raise exception 'not_allowed' using errcode = '42501';
    end if;
    if s is null then
        raise exception 'missing_session';
    end if;
    v_session := (s ->> 'id')::uuid;

    insert into public.game_sessions
        (id, game_id, started_at, ended_at, duration_sec, paused_sec, completed, difficulty, level)
    values (
        v_session, (s ->> 'game_id')::uuid, (s ->> 'started_at')::timestamptz, (s ->> 'ended_at')::timestamptz,
        (s ->> 'duration_sec')::int, coalesce((s ->> 'paused_sec')::int, 0),
        coalesce((s ->> 'completed')::boolean, false), s ->> 'difficulty', (s ->> 'level')::int)
    on conflict (id) do nothing;

    insert into public.game_session_participants (session_id, profile_id, player_slot)
    values (v_session, v_profile, 1)
    on conflict (session_id, player_slot) do nothing;

    -- si el id de sesion ya existia y era de OTRO niño, se corta y se revierte todo
    if not exists (select 1 from public.game_session_participants
                    where session_id = v_session and profile_id = v_profile) then
        raise exception 'session_belongs_to_another_profile' using errcode = '42501';
    end if;

    -- las metricas cuelgan del participante (el niño de esta sesion), no de la sesion
    select id into v_participant
      from public.game_session_participants
     where session_id = v_session and profile_id = v_profile;

    if m is not null then
        insert into public.metrics (id, participant_id, correct_answers, wrong_answers, hints_used, recorded_at)
        values ((m ->> 'id')::uuid, v_participant, (m ->> 'correct_answers')::int, (m ->> 'wrong_answers')::int,
                coalesce((m ->> 'hints_used')::int, 0), coalesce((m ->> 'recorded_at')::timestamptz, now()))
        on conflict (id) do nothing;
    end if;

    for d in select * from jsonb_array_elements(coalesce(payload -> 'details', '[]'::jsonb)) loop
        insert into public.game_metrics_detail (id, participant_id, metric_key, metric_value)
        values ((d ->> 'id')::uuid, v_participant, d ->> 'metric_key', (d ->> 'metric_value')::float)
        on conflict (participant_id, metric_key) do update set metric_value = excluded.metric_value;
    end loop;
end;
$$;

-- Solo usuarios autenticados pueden ejecutar las funciones de la app
revoke all on function public.create_link_code(uuid, int)   from public, anon;
revoke all on function public.redeem_link_code(text)        from public, anon;
revoke all on function public.revoke_assignment(uuid)       from public, anon;
revoke all on function public.upload_game_session(jsonb)    from public, anon;
revoke all on function public.review_professional(uuid, text, text, text, text) from public, anon;
grant execute on function public.review_professional(uuid, text, text, text, text) to authenticated;   -- solo actua si is_admin()
grant execute on function public.create_link_code(uuid, int)  to authenticated;
grant execute on function public.redeem_link_code(text)       to authenticated;
grant execute on function public.revoke_assignment(uuid)      to authenticated;
grant execute on function public.upload_game_session(jsonb)   to authenticated;
