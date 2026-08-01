-- DeerStand RLS policies (applied via EF migration AddRowLevelSecurity).
-- Membership set is resolved through a SECURITY DEFINER helper so club_members
-- RLS does not recurse when evaluating other policies.

CREATE OR REPLACE FUNCTION app_current_profile_id()
RETURNS text
LANGUAGE sql
STABLE
AS $$
  SELECT NULLIF(current_setting('app.current_profile_id', true), '');
$$;

CREATE OR REPLACE FUNCTION app_user_club_ids()
RETURNS SETOF uuid
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT club_id
  FROM club_members
  WHERE profile_id = app_current_profile_id();
$$;

REVOKE ALL ON FUNCTION app_user_club_ids() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION app_user_club_ids() TO PUBLIC;
GRANT EXECUTE ON FUNCTION app_current_profile_id() TO PUBLIC;

-- clubs
ALTER TABLE clubs ENABLE ROW LEVEL SECURITY;
ALTER TABLE clubs FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS clubs_tenant_select ON clubs;
CREATE POLICY clubs_tenant_select ON clubs
  FOR SELECT
  USING (id IN (SELECT app_user_club_ids()));

DROP POLICY IF EXISTS clubs_tenant_insert ON clubs;
CREATE POLICY clubs_tenant_insert ON clubs
  FOR INSERT
  WITH CHECK (owner_id = app_current_profile_id());

DROP POLICY IF EXISTS clubs_tenant_update ON clubs;
CREATE POLICY clubs_tenant_update ON clubs
  FOR UPDATE
  USING (id IN (SELECT app_user_club_ids()))
  WITH CHECK (id IN (SELECT app_user_club_ids()));

DROP POLICY IF EXISTS clubs_tenant_delete ON clubs;
CREATE POLICY clubs_tenant_delete ON clubs
  FOR DELETE
  USING (id IN (SELECT app_user_club_ids()) AND owner_id = app_current_profile_id());

-- club_members
ALTER TABLE club_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE club_members FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS club_members_tenant_select ON club_members;
CREATE POLICY club_members_tenant_select ON club_members
  FOR SELECT
  USING (club_id IN (SELECT app_user_club_ids()));

DROP POLICY IF EXISTS club_members_tenant_insert ON club_members;
CREATE POLICY club_members_tenant_insert ON club_members
  FOR INSERT
  WITH CHECK (
    profile_id = app_current_profile_id()
    OR club_id IN (SELECT app_user_club_ids())
  );

DROP POLICY IF EXISTS club_members_tenant_update ON club_members;
CREATE POLICY club_members_tenant_update ON club_members
  FOR UPDATE
  USING (club_id IN (SELECT app_user_club_ids()))
  WITH CHECK (club_id IN (SELECT app_user_club_ids()));

DROP POLICY IF EXISTS club_members_tenant_delete ON club_members;
CREATE POLICY club_members_tenant_delete ON club_members
  FOR DELETE
  USING (club_id IN (SELECT app_user_club_ids()));

-- stands
ALTER TABLE stands ENABLE ROW LEVEL SECURITY;
ALTER TABLE stands FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS stands_tenant ON stands;
CREATE POLICY stands_tenant ON stands
  FOR ALL
  USING (club_id IN (SELECT app_user_club_ids()))
  WITH CHECK (club_id IN (SELECT app_user_club_ids()));

-- active_check_ins (via stand club)
ALTER TABLE active_check_ins ENABLE ROW LEVEL SECURITY;
ALTER TABLE active_check_ins FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS active_check_ins_tenant ON active_check_ins;
CREATE POLICY active_check_ins_tenant ON active_check_ins
  FOR ALL
  USING (
    stand_id IN (
      SELECT id FROM stands WHERE club_id IN (SELECT app_user_club_ids())
    )
  )
  WITH CHECK (
    stand_id IN (
      SELECT id FROM stands WHERE club_id IN (SELECT app_user_club_ids())
    )
  );

-- check_in_history (via stand club)
ALTER TABLE check_in_history ENABLE ROW LEVEL SECURITY;
ALTER TABLE check_in_history FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS check_in_history_tenant ON check_in_history;
CREATE POLICY check_in_history_tenant ON check_in_history
  FOR ALL
  USING (
    stand_id IN (
      SELECT id FROM stands WHERE club_id IN (SELECT app_user_club_ids())
    )
  )
  WITH CHECK (
    stand_id IN (
      SELECT id FROM stands WHERE club_id IN (SELECT app_user_club_ids())
    )
  );

-- activity_logs
ALTER TABLE activity_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE activity_logs FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS activity_logs_tenant ON activity_logs;
CREATE POLICY activity_logs_tenant ON activity_logs
  FOR ALL
  USING (club_id IN (SELECT app_user_club_ids()))
  WITH CHECK (club_id IN (SELECT app_user_club_ids()));

-- profiles: callers see their own row; clubmate visibility is app-layer for now
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE profiles FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS profiles_self ON profiles;
CREATE POLICY profiles_self ON profiles
  FOR ALL
  USING (id = app_current_profile_id())
  WITH CHECK (id = app_current_profile_id());

DROP POLICY IF EXISTS profiles_clubmates_select ON profiles;
CREATE POLICY profiles_clubmates_select ON profiles
  FOR SELECT
  USING (
    id IN (
      SELECT cm.profile_id
      FROM club_members cm
      WHERE cm.club_id IN (SELECT app_user_club_ids())
    )
  );
