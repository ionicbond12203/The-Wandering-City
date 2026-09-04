'use client';
import React, { useState, useEffect, useCallback } from 'react';
import {
  Compass,
  Map,
  Backpack,
  Hammer,
  Settings,
  X,
  Leaf,
  Gem,
  Swords,
  Tent,
  Trees,
  Mountain,
  FlaskConical,
  ChevronRight,
  ArrowUpRight,
  Check,
  Navigation,
  Plus,
  Shield,
  Heart,
  Flag,
  RotateCcw,
  CircleHelp,
  Home,
  Layers,
  Triangle,
  Sparkles,
} from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import {
  initialState,
  transition,
  locations,
  items,
  objective,
  restore,
} from './demo-model.mjs';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogTitle,
  AlertDialogDescription,
} from '@/components/ui/alert-dialog';
import './demo.css';
const icons = {
  leaf: Leaf,
  gem: Gem,
  swords: Swords,
  tent: Tent,
  trees: Trees,
  mountain: Mountain,
  flask: FlaskConical,
};
function Icon({ name, ...props }) {
  const C = icons[name] || Compass;
  return <C {...props} />;
}
const storageKey = 'wandering-city-demo-v1';
export default function Demo() {
  const [state, setState] = useState(initialState);
  const [ready, setReady] = useState(false);
  const [panel, setPanel] = useState(null);
  const [selected, setSelected] = useState('wood');
  const [notice, setNotice] = useState(null);
  const [workTab, setWorkTab] = useState('craft');
  const [resetOpen, setResetOpen] = useState(false);
  const [mapSelection, setMapSelection] = useState('field');
  const [impact, setImpact] = useState(false);
  useEffect(() => {
    const frame = requestAnimationFrame(() => {
      try {
        setState(restore(localStorage.getItem(storageKey)));
      } catch {}
      setReady(true);
    });
    return () => cancelAnimationFrame(frame);
  }, []);
  useEffect(() => {
    if (!ready) return;
    const timer = setTimeout(() => {
      try {
        localStorage.setItem(storageKey, JSON.stringify(state));
      } catch {
        setNotice({
          message: '浏览器无法保存进度，本次仍可继续体验',
          ok: false,
        });
      }
    }, 0);
    return () => clearTimeout(timer);
  }, [state, ready]);
  useEffect(() => {
    if (!notice) return;
    const timer = setTimeout(() => setNotice(null), 3400);
    return () => clearTimeout(timer);
  }, [notice]);
  const act = useCallback(
    (action) => {
      const result = transition(state, action);
      setState(result.state);
      setNotice({ message: result.message, ok: result.ok });
      if (action.type === 'attack' && result.ok) {
        setImpact(true);
        setTimeout(() => setImpact(false), 250);
      }
    },
    [state],
  );
  const open = useCallback(
    (name) => {
      setPanel((p) => (p === name ? null : name));
      if (name === 'map') setMapSelection(state.location);
    },
    [state.location],
  );
  const interact = useCallback(() => {
    if (state.location === 'camp') setPanel('workshop');
    else if (state.location === 'ruins')
      act({ type: state.enemyHp > 0 ? 'attack' : 'claim' });
    else act({ type: 'gather' });
  }, [state, act]);
  useEffect(() => {
    const onKey = (e) => {
      if (
        e.repeat ||
        /INPUT|TEXTAREA|SELECT/.test(e.target.tagName) ||
        e.ctrlKey ||
        e.metaKey ||
        e.altKey
      )
        return;
      if (e.code === 'Escape') {
        setPanel(null);
        return;
      }
      if (panel || resetOpen) return;
      const actions = {
        KeyB: () => open('bag'),
        KeyM: () => open('map'),
        KeyC: () => open('workshop'),
        KeyE: interact,
        KeyJ: () => act({ type: 'attack' }),
        Space: () => act({ type: 'dodge' }),
        KeyQ: () => act({ type: 'heal' }),
      };
      if (actions[e.code]) {
        e.preventDefault();
        actions[e.code]();
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [panel, resetOpen, open, interact, act]);
  const place = locations.find((l) => l.id === state.location);
  const goal = objective(state);
  const isCombat = state.location === 'ruins' && state.enemyHp > 0;
  const travel = (to) => {
    act({ type: 'travel', to });
    setPanel(null);
  };
  const panelTitles = {
    bag: '行囊',
    map: '原野地图',
    workshop: '据点工作台',
    build: '搭建你的归处',
    settings: '冒险设置',
    help: '旅人指南',
  };
  return (
    <main className={`game scene-${state.location} ${impact ? 'impact' : ''}`}>
      <div className="landscape" aria-hidden="true" />
      <div className="scene-shade" />
      <header className="topbar">
        <button
          className="brand"
          onClick={() => open('help')}
          aria-label="打开旅人指南"
        >
          <span className="brand-symbol">
            <Compass />
          </span>
          <span>
            逐风之旅<small>THE WANDERING CITY</small>
          </span>
        </button>
        <div className="chapter">
          <span className="tiny-diamond" />
          第一章 · 风起之地
          <span className="chapter-line" />
        </div>
        <nav className="topnav" aria-label="冒险菜单">
          {[
            ['map', Map, '地图', 'M'],
            ['bag', Backpack, '背包', 'B'],
            ['workshop', Hammer, '工作台', 'C'],
          ].map(([name, C, label, key]) => (
            <button
              key={name}
              className={panel === name ? 'nav-button active' : 'nav-button'}
              onClick={() => open(name)}
            >
              <C />
              <span>{label}</span>
              <kbd>{key}</kbd>
            </button>
          ))}
          <span className="nav-divider" />
          <button
            className="icon-button"
            onClick={() => open('settings')}
            aria-label="打开设置"
          >
            <Settings />
          </button>
        </nav>
      </header>
      <section className="left-hud">
        <button
          className="minimap"
          onClick={() => open('map')}
          aria-label="打开原野地图"
        >
          <div className="map-grid" />
          <span className="north">N</span>
          <span className="map-trail" />
          <span className="mini-marker camp-marker">
            <Tent size={17} />
          </span>
          <span className="mini-marker ruins-marker">
            <Flag size={15} />
          </span>
          <span className="player-marker">
            <Navigation size={21} fill="currentColor" />
          </span>
          <span className="minimap-caption">
            <Map size={13} /> 风起之地
          </span>
        </button>
        <div className="quest">
          <div className="eyebrow">
            <span className="gold-diamond">◆</span> 主线任务{' '}
            <span className="quest-count">{Math.min(goal.step, 5)} / 5</span>
          </div>
          <h2>{goal.title}</h2>
          <p>{goal.detail}</p>
          <button
            onClick={() => {
              setMapSelection(goal.target);
              setPanel('map');
            }}
            className="text-action"
          >
            {goal.step === 6 ? '继续探索' : '查看目的地'}
            <ChevronRight size={15} />
          </button>
          <div className="quest-progress">
            {[1, 2, 3, 4, 5].map((i) => (
              <i
                key={i}
                className={
                  goal.step > i ? 'done' : goal.step === i ? 'current' : ''
                }
              />
            ))}
          </div>
        </div>
        <div className="region-card">
          <span>区域探索</span>
          <strong>
            {state.explored.length * 25}
            <small>%</small>
          </strong>
          <div>
            <i style={{ width: `${state.explored.length * 25}%` }} />
          </div>
          <p>{state.explored.length} / 4 地点已发现</p>
        </div>
      </section>
      <section className="scene-heading" aria-label="当前位置">
        <div className="location-rule" />
        <p>{place.en}</p>
        <h1>{place.name}</h1>
        <span>{place.subtitle}</span>
      </section>
      <div className="weather">
        <span className="sun-symbol">☀</span>
        <div>
          晴朗<small>16:28 · 微风</small>
        </div>
      </div>
      {isCombat ? (
        <section className="enemy-card">
          <div>
            <Swords size={18} />
            <span>遗迹守卫</span>
            <small>Lv. 1</small>
          </div>
          <div className="enemy-health">
            <i style={{ width: `${state.enemyHp}%` }} />
          </div>
          <p>
            {state.enemyHp} / 100 {state.guarded ? '· 闪避就绪' : '· 警戒中'}
          </p>
        </section>
      ) : (
        <button className="world-pin" onClick={interact}>
          <span className="pin-glow">
            <Icon
              name={state.location === 'ruins' ? 'gem' : place.icon}
              size={27}
            />
          </span>
          <span>
            {state.location === 'field'
              ? '风纹古树'
              : state.location === 'quarry'
                ? '散落的石堆'
                : state.location === 'ruins'
                  ? state.claimed
                    ? '已探索的遗迹'
                    : '遗迹宝箱'
                  : '营地工作台'}
          </span>
          <small>
            {state.location === 'ruins' && state.claimed
              ? '奖励已领取'
              : '点击交互'}
          </small>
        </button>
      )}
      <aside className="right-hud">
        <div className="adventure-level">
          <Sparkles size={16} />
          <span>冒险等阶</span>
          <strong>01</strong>
        </div>
        <button
          className="destination-card"
          onClick={() => {
            setMapSelection(goal.target);
            setPanel('map');
          }}
        >
          <span className="destination-icon">
            <Icon name={locations.find((l) => l.id === goal.target).icon} />
          </span>
          <span>
            <small>{goal.step === 6 ? '下一段故事' : '旅途指引'}</small>
            <strong>{locations.find((l) => l.id === goal.target).name}</strong>
          </span>
          <ArrowUpRight size={17} />
        </button>
      </aside>
      <div className="interaction-prompt">
        <span className="interaction-line" />
        <button
          onClick={interact}
          disabled={state.location === 'ruins' && state.claimed}
        >
          <kbd>E</kbd>
          <span>
            {state.location === 'field'
              ? '采集风纹木材'
              : state.location === 'quarry'
                ? '采集原野石材'
                : state.location === 'camp'
                  ? '使用工作台'
                  : state.claimed
                    ? '营地已清理'
                    : isCombat
                      ? '攻击遗迹守卫'
                      : '领取营地奖励'}
          </span>
          {state.location === 'field' || state.location === 'quarry' ? (
            <small>+3</small>
          ) : (
            <ChevronRight size={16} />
          )}
        </button>
      </div>
      <footer className="bottom-hud">
        <section className="player-info">
          <span className="avatar">
            <Compass size={29} />
          </span>
          <div className="player-details">
            <div>
              <strong>旅人 · 岚</strong>
              <span>Lv. {state.level}</span>
            </div>
            <div className="health-track">
              <i style={{ width: `${state.hp}%` }} />
            </div>
            <small>
              <Heart size={12} /> {state.hp} / 100{' '}
              <span>旅人长剑 · 攻击 {state.level === 1 ? 26 : 42}</span>
            </small>
          </div>
        </section>
        <div className="quick-actions">
          <button
            className="quick-item"
            onClick={() => act({ type: 'heal' })}
            aria-label={`使用晨露药剂，剩余 ${state.potion}`}
          >
            <FlaskConical />
            <span className="item-count">{state.potion}</span>
            <kbd>Q</kbd>
          </button>
          <span className="quick-divider" />
          <button
            className={`round-action ${isCombat ? 'combat-ready' : ''}`}
            disabled={!isCombat}
            onClick={() => act({ type: 'attack' })}
            aria-label="普通攻击"
          >
            <Swords />
            <kbd>J</kbd>
            <span>攻击</span>
          </button>
          <button
            className={`round-action ${state.guarded ? 'guarded' : ''}`}
            disabled={!isCombat}
            onClick={() => act({ type: 'dodge' })}
            aria-label="闪避"
          >
            <Shield />
            <kbd>空格</kbd>
            <span>闪避</span>
          </button>
        </div>
      </footer>
      <div className="bottom-meta">
        <span>
          <span className="live-dot" /> 界面体验版{' '}
          <span className="meta-sep">/</span> 本地进度
        </span>
        <button onClick={() => open('help')}>
          <CircleHelp size={14} /> 操作指南
        </button>
        <span className="build-label">WINDRISE · 001</span>
      </div>
      <output
        className={`toast ${notice ? 'visible' : ''} ${notice?.ok ? '' : 'warning'}`}
        aria-live="polite"
      >
        {notice?.ok ? <Check size={18} /> : <CircleHelp size={18} />}
        {notice?.message}
      </output>
      <Dialog
        open={!!panel}
        onOpenChange={(value) => {
          if (!value) setPanel(null);
        }}
      >
        <DialogContent
          className={`game-dialog ${panel === 'map' ? 'map-dialog' : ''}`}
          showCloseButton={false}
        >
          <header className="panel-header">
            <div>
              <p>THE WANDERING CITY</p>
              <DialogTitle>{panelTitles[panel] || '冒险菜单'}</DialogTitle>
            </div>
            <button
              className="panel-close"
              onClick={() => setPanel(null)}
              aria-label="关闭窗口"
            >
              <X />
            </button>
          </header>
          <DialogDescription className="sr-only">
            查看并操作{panelTitles[panel]}。按 Escape 关闭。
          </DialogDescription>
          {panel === 'bag' && (
            <div className="bag-layout">
              <section>
                <div className="section-top">
                  <span>全部物品</span>
                  <small>
                    {Object.keys(items).filter((k) => state[k] > 0).length} / 24
                    格
                  </small>
                </div>
                <div className="inventory-grid">
                  {Object.entries(items).map(([key, item]) => (
                    <button
                      key={key}
                      className={`inventory-item ${selected === key ? 'selected' : ''} ${key === 'ore' ? 'rare' : ''}`}
                      onClick={() => setSelected(key)}
                      aria-label={`${item.name} ${state[key]} 个`}
                    >
                      <Icon name={item.icon} />
                      <span>{item.name}</span>
                      <strong>×{state[key]}</strong>
                    </button>
                  ))}
                  {[1, 2, 3, 4].map((n) => (
                    <div className="empty-slot" key={n}>
                      ＋
                    </div>
                  ))}
                </div>
                <div className="equipped">
                  <Swords />
                  <div>
                    <strong>旅人长剑</strong>
                    <small>当前装备 · Lv.{state.level}</small>
                  </div>
                  <span>攻击 {state.level === 1 ? 26 : 42}</span>
                </div>
              </section>
              <aside className="item-detail">
                <div
                  className={`detail-art ${selected === 'ore' ? 'rare' : ''}`}
                >
                  <Icon name={items[selected].icon} size={66} />
                </div>
                <p className="item-rarity">{items[selected].rarity}</p>
                <h3>{items[selected].name}</h3>
                <p>{items[selected].description}</p>
                <div className="detail-quantity">
                  持有数量 <strong>{state[selected]}</strong>
                </div>
                {selected === 'potion' ? (
                  <button
                    className="primary-button"
                    disabled={!state.potion || state.hp === 100}
                    onClick={() => act({ type: 'heal' })}
                  >
                    {state.hp === 100 ? '生命已满' : '使用药剂'}
                  </button>
                ) : (
                  <button
                    className="secondary-button"
                    onClick={() => {
                      setMapSelection(
                        selected === 'wood'
                          ? 'field'
                          : selected === 'stone'
                            ? 'quarry'
                            : 'ruins',
                      );
                      setPanel('map');
                    }}
                  >
                    查看获取地点
                    <ArrowUpRight size={16} />
                  </button>
                )}
              </aside>
            </div>
          )}
          {panel === 'map' && (
            <div className="map-layout">
              <div className="world-map">
                <div className="map-texture" />
                <span className="map-region-label">
                  风 起 之 地<small>WINDRISE</small>
                </span>
                <svg
                  className="map-paths"
                  viewBox="0 0 100 100"
                  preserveAspectRatio="none"
                  aria-hidden="true"
                >
                  <path d="M24 30 Q15 50 32 62 T75 60 Q85 40 64 28 T24 30" />
                </svg>
                {locations.map((l) => (
                  <button
                    key={l.id}
                    className={`map-location ${mapSelection === l.id ? 'selected' : ''} ${state.location === l.id ? 'here' : ''}`}
                    style={{ left: `${l.x}%`, top: `${l.y}%` }}
                    onClick={() => setMapSelection(l.id)}
                  >
                    <span>
                      <Icon name={l.icon} />
                    </span>
                    <strong>{l.name}</strong>
                    {state.location === l.id && <small>当前位置</small>}
                  </button>
                ))}
                <div className="map-legend">
                  <Navigation size={15} /> 点击地点查看详情{' '}
                  <span>◇ 已发现 {state.explored.length}/4</span>
                </div>
              </div>
              <aside className="map-detail">
                <span className="section-kicker">
                  {locations.find((l) => l.id === mapSelection).type}
                </span>
                <Icon
                  name={locations.find((l) => l.id === mapSelection).icon}
                  size={40}
                />
                <h3>{locations.find((l) => l.id === mapSelection).name}</h3>
                <p>
                  {locations.find((l) => l.id === mapSelection).description}
                </p>
                <div className="place-status">
                  {state.explored.includes(mapSelection) ? (
                    <>
                      <Check size={15} /> 已发现
                    </>
                  ) : (
                    <>
                      <Compass size={15} /> 尚未探索
                    </>
                  )}
                </div>
                <button
                  className="primary-button"
                  disabled={mapSelection === state.location}
                  onClick={() => travel(mapSelection)}
                >
                  {mapSelection === state.location
                    ? '当前所在区域'
                    : '前往这里'}
                  <ArrowUpRight size={16} />
                </button>
                <small className="travel-note">
                  本次体验直接切换到所选区域
                </small>
              </aside>
            </div>
          )}
          {panel === 'workshop' && (
            <div className="workshop-content">
              <div className="workshop-intro">
                <span className="workshop-mark">
                  <Hammer size={35} />
                </span>
                <div>
                  <h3>让每一次出发，都更从容。</h3>
                  <p>制作旅途补给，唤醒武器中的力量。</p>
                </div>
                <span className="camp-status">
                  {state.location === 'camp' ? '已在据点' : '需要返回据点'}
                </span>
              </div>
              {state.location !== 'camp' && (
                <div className="location-notice">
                  工作台位于旅人据点。
                  <button onClick={() => act({ type: 'travel', to: 'camp' })}>
                    返回据点
                    <ArrowUpRight size={14} />
                  </button>
                </div>
              )}
              <Tabs value={workTab} onValueChange={setWorkTab}>
                <div className="work-tabs">
                  <TabsList variant="line">
                    <TabsTrigger value="craft">补给制作</TabsTrigger>
                    <TabsTrigger value="upgrade">武器强化</TabsTrigger>
                  </TabsList>
                  <button onClick={() => setPanel('build')}>
                    据点建造
                    <ArrowUpRight size={14} />
                  </button>
                </div>
                <TabsContent value={workTab}>
                  <div className="recipe">
                    <div className="recipe-art">
                      {workTab === 'craft' ? (
                        <FlaskConical size={70} />
                      ) : (
                        <Swords size={70} />
                      )}
                    </div>
                    <div className="recipe-body">
                      <span className="section-kicker">
                        {workTab === 'craft' ? '恢复道具' : '旅人装备'}
                      </span>
                      <h3>{workTab === 'craft' ? '晨露药剂' : '旅人长剑'}</h3>
                      <p>
                        {workTab === 'craft'
                          ? '凝聚晨间露水的清新药剂。恢复 45 点生命。'
                          : '用星辉矿石强化剑锋。攻击力从 26 提升至 42。'}
                      </p>
                      <div className="recipe-costs">
                        {(workTab === 'craft'
                          ? [
                              ['wood', 2],
                              ['stone', 1],
                            ]
                          : [['ore', 3]]
                        ).map(([key, cost]) => (
                          <span
                            key={key}
                            className={state[key] < cost ? 'insufficient' : ''}
                          >
                            <Icon name={items[key].icon} size={17} />
                            {items[key].name}
                            <strong>
                              {state[key]} / {cost}
                            </strong>
                          </span>
                        ))}
                      </div>
                      <button
                        className="primary-button"
                        disabled={
                          state.location !== 'camp' ||
                          (workTab === 'craft'
                            ? state.wood < 2 || state.stone < 1
                            : state.ore < 3 || state.level > 1)
                        }
                        onClick={() =>
                          act({
                            type: workTab === 'craft' ? 'craft' : 'upgrade',
                          })
                        }
                      >
                        {workTab === 'craft'
                          ? '制作药剂'
                          : state.level > 1
                            ? '已强化至 Lv.2'
                            : '强化至 Lv.2'}
                        <Sparkles size={16} />
                      </button>
                    </div>
                  </div>
                </TabsContent>
              </Tabs>
            </div>
          )}
          {panel === 'build' && (
            <div className="build-content">
              <div className="section-top">
                <span>旅人据点 · {state.buildings.length} / 6 模块</span>
                <span>
                  <Trees size={17} /> 木材 {state.wood}
                </span>
              </div>
              {state.location !== 'camp' && (
                <div className="location-notice">
                  需要在据点范围内建造。
                  <button onClick={() => act({ type: 'travel', to: 'camp' })}>
                    返回据点
                  </button>
                </div>
              )}
              <div className="build-slots">
                {Array.from({ length: 6 }, (_, i) => (
                  <div
                    className={
                      state.buildings[i] ? 'build-slot placed' : 'build-slot'
                    }
                    key={i}
                  >
                    {state.buildings[i] ? (
                      <>
                        <Home size={30} />
                        <strong>{state.buildings[i]}</strong>
                        <button
                          onClick={() => act({ type: 'remove', index: i })}
                          disabled={state.location !== 'camp'}
                        >
                          回收 · 木材 +2
                        </button>
                      </>
                    ) : (
                      <>
                        <Plus size={23} />
                        <span>空地 {i + 1}</span>
                      </>
                    )}
                  </div>
                ))}
              </div>
              <div className="build-options">
                {[
                  ['地板', Layers],
                  ['墙体', Home],
                  ['屋顶', Triangle],
                ].map(([part, C]) => (
                  <button
                    key={part}
                    disabled={
                      state.location !== 'camp' ||
                      state.wood < 2 ||
                      state.buildings.length >= 6
                    }
                    onClick={() => act({ type: 'build', part })}
                  >
                    <C />
                    <span>
                      <strong>放置{part}</strong>
                      <small>木材 ×2</small>
                    </span>
                    <Plus size={18} />
                  </button>
                ))}
              </div>
              <p className="subtle-note">
                点击模块即可放置到空地；回收会返还全部材料。
              </p>
            </div>
          )}
          {panel === 'settings' && (
            <div className="settings-content">
              <div className="settings-row">
                <div>
                  <h3>冒险进度</h3>
                  <p>自动保存在当前浏览器，下次打开可继续。</p>
                </div>
                <span className="save-label">
                  <Check size={15} /> 本地自动保存
                </span>
              </div>
              <div className="settings-row">
                <div>
                  <h3>重新开始旅程</h3>
                  <p>清空当前浏览器中的物品、任务与据点进度。</p>
                </div>
                <button
                  className="secondary-button"
                  onClick={() => setResetOpen(true)}
                >
                  <RotateCcw size={16} />
                  重新开始
                </button>
              </div>
              <div className="demo-note">
                <Compass />
                <div>
                  <strong>界面与流程体验版</strong>
                  <p>
                    场景切换与战斗使用模拟数据。本版用于体验操作流程，不包含自由
                    3D 移动和真实后端。
                  </p>
                </div>
              </div>
            </div>
          )}
          {panel === 'help' && (
            <div className="help-content">
              <h3>向着风的方向，出发。</h3>
              <p>跟随左侧主线提示，在地图中选择地点，再与场景中的目标交互。</p>
              <div className="help-steps">
                {[
                  ['01', '采集', '原野采木，石场采石'],
                  ['02', '准备', '回据点制作恢复药剂'],
                  ['03', '挑战', '击败守卫并领取矿石'],
                  ['04', '成长', '强化武器，搭建小屋'],
                ].map(([n, title, text]) => (
                  <div key={n}>
                    <span>{n}</span>
                    <strong>{title}</strong>
                    <p>{text}</p>
                  </div>
                ))}
              </div>
              <div className="key-list">
                {[
                  ['M', '地图'],
                  ['B', '背包'],
                  ['C', '工作台'],
                  ['E', '场景交互'],
                  ['J', '攻击'],
                  ['空格', '闪避下一次反击'],
                  ['Q', '使用药剂'],
                  ['Esc', '关闭窗口'],
                ].map(([key, text]) => (
                  <span key={key}>
                    <kbd>{key}</kbd>
                    {text}
                  </span>
                ))}
              </div>
              <p className="subtle-note">
                所有操作也可点击完成。战斗按回合模拟：先闪避，再攻击可免受本次反击。
              </p>
              <button className="primary-button" onClick={() => setPanel(null)}>
                继续冒险
                <ChevronRight size={17} />
              </button>
            </div>
          )}
          <div className="panel-footer">
            <span>风起之地 · 旅人的手记</span>
            <button onClick={() => setPanel(null)}>
              <kbd>Esc</kbd> 返回冒险
            </button>
          </div>
        </DialogContent>
      </Dialog>
      <AlertDialog open={resetOpen} onOpenChange={setResetOpen}>
        <AlertDialogContent className="game-dialog reset-dialog">
          <AlertDialogTitle>重新开始旅程？</AlertDialogTitle>
          <AlertDialogDescription>
            当前浏览器中的冒险进度将被清空，此操作无法撤销。
          </AlertDialogDescription>
          <div className="reset-actions">
            <button
              className="secondary-button"
              onClick={() => setResetOpen(false)}
            >
              继续当前旅程
            </button>
            <button
              className="primary-button"
              onClick={() => {
                setState(initialState());
                setResetOpen(false);
                setPanel(null);
                setNotice({ ok: true, message: '新的旅程已经开始' });
              }}
            >
              确认重新开始
            </button>
          </div>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  );
}
