export const locations = [
  {
    id: 'field',
    name: '风息原野',
    en: 'WINDRISE MEADOW',
    subtitle: '循着风的方向，开始新的旅程。',
    icon: 'leaf',
    type: '采集区域',
    description: '风车草与古树之间，藏着旅途需要的第一份补给。',
    x: 32,
    y: 62,
  },
  {
    id: 'quarry',
    name: '旧日采石场',
    en: 'THE OLD QUARRY',
    subtitle: '古老的石壁，仍回响着开拓者的故事。',
    icon: 'gem',
    type: '资源区域',
    description: '从散落的石堆中收集石材，为据点的工作台准备材料。',
    x: 64,
    y: 28,
  },
  {
    id: 'ruins',
    name: '遗迹营地',
    en: 'SILENT RUINS',
    subtitle: '穿过断壁，寻找被遗忘的星辉。',
    icon: 'swords',
    type: '战斗区域',
    description: '击败遗迹守卫，领取用于武器升级的星辉矿石。',
    x: 75,
    y: 60,
  },
  {
    id: 'camp',
    name: '旅人据点',
    en: 'TRAVELER’S REST',
    subtitle: '每一次远行，都有可以归来的地方。',
    icon: 'tent',
    type: '安全区域',
    description: '在工作台制作补给、升级武器，或为自己搭建一处小屋。',
    x: 24,
    y: 30,
  },
];
export const items = {
  wood: {
    name: '风纹木材',
    icon: 'trees',
    description: '质地轻盈的木材，可用于制作与据点建造。',
    rarity: '普通素材',
  },
  stone: {
    name: '原野石材',
    icon: 'mountain',
    description: '来自旧日采石场的石材，制作和建造的基础材料。',
    rarity: '普通素材',
  },
  ore: {
    name: '星辉矿石',
    icon: 'gem',
    description: '遗迹中蕴含微光的矿石，可以强化旅人的武器。',
    rarity: '稀有素材',
  },
  potion: {
    name: '晨露药剂',
    icon: 'flask',
    description: '使用后恢复 45 点生命。生命已满时不会消耗。',
    rarity: '恢复道具',
  },
};
export const initialState = () => ({
  version: 1,
  location: 'field',
  hp: 100,
  wood: 0,
  stone: 0,
  ore: 0,
  potion: 1,
  level: 1,
  enemyHp: 100,
  guarded: false,
  claimed: false,
  crafted: false,
  gatheredWood: false,
  gatheredStone: false,
  buildings: [],
  explored: ['field'],
});
export function objective(s) {
  if (!s.gatheredWood)
    return {
      title: '旅途的第一份补给',
      detail: '在风息原野采集风纹木材',
      step: 1,
      target: 'field',
    };
  if (!s.gatheredStone)
    return {
      title: '寻找坚实的材料',
      detail: '前往旧日采石场，采集石材',
      step: 2,
      target: 'quarry',
    };
  if (!s.crafted)
    return {
      title: '为冒险做好准备',
      detail: '回到旅人据点，制作晨露药剂',
      step: 3,
      target: 'camp',
    };
  if (!s.claimed)
    return {
      title: '沉睡遗迹的回响',
      detail: s.enemyHp ? '前往遗迹营地，击败遗迹守卫' : '领取营地中的星辉矿石',
      step: 4,
      target: 'ruins',
    };
  if (s.level === 1)
    return {
      title: '让星辉成为你的力量',
      detail: '回到据点，在工作台升级武器',
      step: 5,
      target: 'camp',
    };
  return {
    title: '新的旅程，正在展开',
    detail: '主线已完成 · 继续探索或搭建自己的小屋',
    step: 6,
    target: 'camp',
  };
}
export function transition(state, action) {
  const s = structuredClone(state);
  const fail = (message) => ({ state, message, ok: false });
  let message = '';
  switch (action.type) {
    case 'travel':
      if (!locations.some((l) => l.id === action.to))
        return fail('目的地不存在');
      s.location = action.to;
      s.guarded = false;
      if (!s.explored.includes(action.to)) s.explored.push(action.to);
      if (action.to === 'camp') s.hp = 100;
      message = `已抵达${locations.find((l) => l.id === action.to).name}${action.to === 'camp' ? ' · 生命已恢复' : ''}`;
      break;
    case 'gather':
      if (s.location === 'field') {
        s.wood += 3;
        s.gatheredWood = true;
        message = '获得 风纹木材 ×3';
      } else if (s.location === 'quarry') {
        s.stone += 3;
        s.gatheredStone = true;
        message = '获得 原野石材 ×3';
      } else return fail('这里没有可采集的资源');
      break;
    case 'attack':
      if (s.location !== 'ruins' || s.enemyHp <= 0) return fail('附近没有敌人');
      s.enemyHp = Math.max(0, s.enemyHp - (s.level === 1 ? 26 : 42));
      const damage = s.enemyHp === 0 || s.guarded ? 0 : 18;
      s.hp = Math.max(0, s.hp - damage);
      s.guarded = false;
      message =
        s.enemyHp === 0
          ? '守卫已击败 · 可以领取营地奖励'
          : damage
            ? `命中守卫 · 受到反击 −${damage} 生命`
            : '命中守卫 · 成功闪避反击';
      if (s.hp === 0) {
        s.hp = 100;
        s.location = 'camp';
        s.enemyHp = 100;
        message = '你已在据点重生 · 背包物品保留';
      }
      break;
    case 'dodge':
      if (s.location !== 'ruins' || s.enemyHp <= 0) return fail('附近没有敌人');
      if (s.guarded) return fail('闪避已就绪 · 现在发动攻击');
      s.guarded = true;
      message = '闪避就绪 · 下一次攻击免受反击';
      break;
    case 'claim':
      if (s.location !== 'ruins' || s.enemyHp > 0 || s.claimed)
        return fail('当前没有可领取的奖励');
      s.claimed = true;
      s.ore += 3;
      message = '获得 星辉矿石 ×3 · 返回据点强化武器';
      break;
    case 'heal':
      if (s.potion <= 0) return fail('药剂不足 · 可以回据点制作');
      if (s.hp === 100) return fail('生命已满，无需使用药剂');
      s.potion -= 1;
      s.hp = Math.min(100, s.hp + 45);
      message = '已使用晨露药剂 · 恢复生命';
      break;
    case 'craft':
      if (s.location !== 'camp') return fail('请先返回旅人据点');
      if (s.wood < 2 || s.stone < 1)
        return fail('材料不足 · 需要木材 ×2、石材 ×1');
      s.wood -= 2;
      s.stone -= 1;
      s.potion++;
      s.crafted = true;
      message = '制作完成 · 晨露药剂 ×1';
      break;
    case 'upgrade':
      if (s.location !== 'camp') return fail('请先返回旅人据点');
      if (s.level > 1) return fail('武器已达到本次冒险的最高等级');
      if (s.ore < 3) return fail('材料不足 · 需要星辉矿石 ×3');
      s.ore -= 3;
      s.level = 2;
      message = '旅人长剑升至 Lv.2 · 攻击力 26 → 42';
      break;
    case 'build':
      if (s.location !== 'camp') return fail('只能在据点范围内建造');
      if (!['地板', '墙体', '屋顶'].includes(action.part))
        return fail('请选择有效的建筑模块');
      if (s.buildings.length >= 6)
        return fail('据点已放满 6 个模块，可先回收已有模块');
      if (s.wood < 2) return fail('材料不足 · 每个模块需要木材 ×2');
      s.wood -= 2;
      s.buildings.push(action.part);
      message = `已放置${action.part} · 木材 −2`;
      break;
    case 'remove':
      if (
        s.location !== 'camp' ||
        !Number.isInteger(action.index) ||
        !s.buildings[action.index]
      )
        return fail('请选择已有的建筑模块');
      const part = s.buildings.splice(action.index, 1)[0];
      s.wood += 2;
      message = `已回收${part} · 木材 +2`;
      break;
    default:
      return fail('未知操作');
  }
  return { state: s, message, ok: true };
}
export function restore(raw) {
  try {
    const s = JSON.parse(raw);
    if (!s || s.version !== 1 || !locations.some((l) => l.id === s.location))
      return initialState();
    for (const key of [
      'wood',
      'stone',
      'ore',
      'potion',
      'hp',
      'enemyHp',
      'level',
    ])
      if (!Number.isInteger(s[key]) || s[key] < 0) return initialState();
    if (
      s.hp > 100 ||
      s.enemyHp > 100 ||
      ![1, 2].includes(s.level) ||
      !Array.isArray(s.buildings) ||
      s.buildings.length > 6 ||
      !s.buildings.every((p) => ['地板', '墙体', '屋顶'].includes(p)) ||
      !Array.isArray(s.explored) ||
      !s.explored.every((id) => locations.some((l) => l.id === id))
    )
      return initialState();
    for (const key of [
      'guarded',
      'claimed',
      'crafted',
      'gatheredWood',
      'gatheredStone',
    ])
      if (typeof s[key] !== 'boolean') return initialState();
    return s;
  } catch {
    return initialState();
  }
}
