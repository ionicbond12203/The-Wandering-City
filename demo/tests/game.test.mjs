import test from 'node:test';
import assert from 'node:assert/strict';
import {
  initialState,
  transition,
  objective,
  restore,
} from '../app/demo-model.mjs';

function run(state, action) {
  const result = transition(state, action);
  assert.equal(result.ok, true, result.message);
  return result.state;
}

test('complete the guided exploration, crafting, combat and upgrade journey', () => {
  let s = initialState();
  assert.equal(objective(s).step, 1);
  s = run(s, { type: 'gather' });
  assert.equal(s.wood, 3);
  assert.equal(objective(s).target, 'quarry');
  s = run(s, { type: 'travel', to: 'quarry' });
  s = run(s, { type: 'gather' });
  s = run(s, { type: 'travel', to: 'camp' });
  s = run(s, { type: 'craft' });
  assert.equal(s.wood, 1);
  assert.equal(s.stone, 2);
  assert.equal(s.potion, 2);
  s = run(s, { type: 'travel', to: 'ruins' });
  for (let i = 0; i < 4; i++) s = run(s, { type: 'attack' });
  assert.equal(s.enemyHp, 0);
  assert.equal(s.hp, 46);
  s = run(s, { type: 'claim' });
  assert.equal(s.ore, 3);
  s = run(s, { type: 'travel', to: 'camp' });
  s = run(s, { type: 'upgrade' });
  assert.equal(s.level, 2);
  assert.equal(s.ore, 0);
  assert.equal(s.hp, 100);
  assert.equal(objective(s).step, 6);
  assert.equal(s.explored.length, 4);
});

test('invalid actions never mutate resources or original state', () => {
  const original = initialState();
  for (const action of [
    { type: 'craft' },
    { type: 'claim' },
    { type: 'upgrade' },
    { type: 'attack' },
    { type: 'dodge' },
    { type: 'build', part: '墙体' },
    { type: 'travel', to: 'invalid' },
  ]) {
    const result = transition(original, action);
    assert.equal(result.ok, false);
    assert.deepEqual(result.state, initialState());
  }
  const camp = { ...original, location: 'camp' };
  assert.equal(transition(camp, { type: 'craft' }).ok, false);
  assert.equal(transition(camp, { type: 'upgrade' }).ok, false);
  assert.deepEqual(original, initialState());
});

test('dodging prevents exactly one counterattack', () => {
  let s = { ...initialState(), location: 'ruins' };
  s = run(s, { type: 'dodge' });
  assert.equal(transition(s, { type: 'dodge' }).ok, false);
  s = run(s, { type: 'attack' });
  assert.equal(s.hp, 100);
  assert.equal(s.guarded, false);
  s = run(s, { type: 'attack' });
  assert.equal(s.hp, 82);
});

test('healing respects inventory and maximum health', () => {
  assert.equal(transition(initialState(), { type: 'heal' }).ok, false);
  const s = run({ ...initialState(), hp: 80 }, { type: 'heal' });
  assert.equal(s.hp, 100);
  assert.equal(s.potion, 0);
  assert.equal(transition({ ...s, hp: 20 }, { type: 'heal' }).ok, false);
});

test('one-time rewards and upgrades cannot be duplicated', () => {
  let s = run(
    { ...initialState(), location: 'ruins', enemyHp: 0 },
    { type: 'claim' },
  );
  assert.equal(transition(s, { type: 'claim' }).ok, false);
  s = run(s, { type: 'travel', to: 'camp' });
  s = run(s, { type: 'upgrade' });
  assert.equal(transition({ ...s, ore: 3 }, { type: 'upgrade' }).ok, false);
});

test('death restores at camp without losing inventory', () => {
  const s = run(
    { ...initialState(), location: 'ruins', hp: 10, wood: 6 },
    { type: 'attack' },
  );
  assert.equal(s.location, 'camp');
  assert.equal(s.hp, 100);
  assert.equal(s.wood, 6);
  assert.equal(s.enemyHp, 100);
});

test('building capacity, safe removal and refunds preserve resource counts', () => {
  let s = { ...initialState(), location: 'camp', wood: 14 };
  for (let i = 0; i < 6; i++) s = run(s, { type: 'build', part: '地板' });
  assert.equal(s.wood, 2);
  assert.equal(transition(s, { type: 'build', part: '墙体' }).ok, false);
  assert.equal(transition(s, { type: 'remove', index: -1 }).ok, false);
  s = run(s, { type: 'remove', index: 2 });
  assert.equal(s.wood, 4);
  assert.equal(s.buildings.length, 5);
  s = run(s, { type: 'build', part: '屋顶' });
  assert.equal(s.wood, 2);
});

test('saves round-trip and malformed saves recover safely', () => {
  const s = { ...initialState(), wood: 8, crafted: true, buildings: ['地板'] };
  assert.deepEqual(restore(JSON.stringify(s)), s);
  for (const raw of [
    null,
    'broken',
    '{}',
    JSON.stringify({ ...s, hp: -1 }),
    JSON.stringify({ ...s, buildings: ['invalid'] }),
    JSON.stringify({ ...s, version: 99 }),
  ])
    assert.deepEqual(restore(raw), initialState());
});
