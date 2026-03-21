using System.Collections;
using UnityEngine;

namespace BossFight
{
    public class ChargeAttack : IBossAttack
    {
        public string Id => "Charge";
        private readonly ChargeConfig cfg;
        private readonly ChargeTelegraph telegraphPrefab;
        private readonly Health playerHP;

        public ChargeAttack(ChargeConfig cfg, ChargeTelegraph telegraphPrefab, Health playerHP)
        {
            this.cfg = cfg;
            this.telegraphPrefab = telegraphPrefab;
            this.playerHP = playerHP;
        }

        public IEnumerator Execute(BossContext C)
{
    // 1. 取得與 LandingAttack 完全一致的高度基準
    // 直接參考玩家當前位置的地面高度，並加上相同的 Offset
    Vector3 playerGround = C.Services.GetGroundBelow(C.Player.position);
    float targetLandHeight = playerGround.y + C.Owner.groundOffset; 

    // 2. 計算水平起點 (保持在正確的高度)
    Vector3 startDir = (C.ModelRoot.position - C.Player.position).normalized;
    Vector3 startPos = playerGround + startDir * C.Owner.MinLandDist;
    startPos.y = targetLandHeight; // 強制設定為與 Landing 一致的高度

    // 3. 執行降落 (流程對齊 LandingAttack)
    // 先水平移動到目標上方 (維持在高空)
    yield return C.Services.MoveHorizontalTo(C.ModelRoot, startPos + Vector3.up * C.Owner.HoverHeight, 35f);
    
    // 垂直砸下：這裡必須使用 MoveTo，目標高度必須是 targetLandHeight
    C.Anim.Play("bird-glide-ani"); 
    yield return C.Services.MoveTo(C.ModelRoot, startPos, C.Owner.DescendSpeed * 1.5f);

    // [重要檢查點] 此時 Boss 應該與 LandingAttack 落地後的高度完全相同

    // 4. 蓄力期間：鎖定 Y 軸，防止動畫偏移
    float timer = 0f;
    Vector3 lockOnDir = C.ModelRoot.forward;
    
    // 如果有 Telegraph，在此生成
    ChargeTelegraph tele = null;
    if (telegraphPrefab != null) {
        tele = Object.Instantiate(telegraphPrefab);
        tele.Setup(C.ModelRoot.position, C.ModelRoot.position, cfg.windup, 0.4f, 0.4f, new Color(1f, 0f, 0f, 0.8f));
    }

    while (timer < cfg.windup)
    {
        // 強制鎖定 Y 座標為剛剛落地的 targetLandHeight，不允許任何高度爬升
        C.ModelRoot.position = new Vector3(C.ModelRoot.position.x, targetLandHeight, C.ModelRoot.position.z);

        Vector3 toPlayer = (C.Player.position - C.ModelRoot.position);
        toPlayer.y = 0;
        if (toPlayer.sqrMagnitude > 0.1f)
        {
            lockOnDir = toPlayer.normalized;
            C.ModelRoot.rotation = Quaternion.Slerp(C.ModelRoot.rotation, Quaternion.LookRotation(lockOnDir), 12f * Time.deltaTime);
        }

        if (tele != null) tele.UpdatePoints(C.ModelRoot.position, C.ModelRoot.position + lockOnDir * cfg.distance);

        timer += Time.deltaTime;
        yield return null;
    }

    // 5. 衝刺執行
    if (tele != null) Object.Destroy(tele.gameObject);
    Vector3 dashEndPos = C.ModelRoot.position + lockOnDir * cfg.distance;
    dashEndPos.y = targetLandHeight; // 確保終點高度一致

    C.Anim.Play("bird-dash-ani"); 
    bool damaged = false;
    while (Vector3.ProjectOnPlane(C.ModelRoot.position - dashEndPos, Vector3.up).sqrMagnitude > 0.1f)
    {
        Vector3 cur = C.ModelRoot.position;
        Vector3 nextXZ = Vector3.MoveTowards(new Vector3(cur.x, 0, cur.z), new Vector3(dashEndPos.x, 0, dashEndPos.z), cfg.speed * Time.deltaTime);
        
        // 衝刺時強制維持在 targetLandHeight
        Vector3 nextPos = new Vector3(nextXZ.x, targetLandHeight, nextXZ.z);

        // 如果地形有高低差才微調 Y
        if (cfg.stickToGround && C.Services.TryProjectToGroundXZ(nextPos, 2f, 2f, out var gp))
        {
            nextPos.y = Mathf.MoveTowards(cur.y, gp.y + C.Owner.groundOffset, 20f * Time.deltaTime);
        }

        if (!damaged) damaged = C.Services.DoCapsuleDamageOnce(cur, nextPos, cfg.width, cfg.damage, playerHP);

        C.ModelRoot.position = nextPos;
        yield return null;
    }

    C.Anim.Play("birld-ani-stun");
    // 6. 結束與起飛
    yield return new WaitForSeconds(cfg.recover);
    C.Anim.Play("bird-fly-ani");
    yield return C.Services.MoveVerticalTo(C.ModelRoot, C.ModelRoot.position.y + C.Owner.HoverHeight, C.Owner.RiseSpeed);
}
    }
}