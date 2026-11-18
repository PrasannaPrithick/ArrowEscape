"""
Hue Gates - reference engine, level generator and verifier.
Pure Python, no dependencies. Port the engine functions to C#/TypeScript.

RULES (summary)
- N x N grid. A cell holds at most one arrow: direction U/D/L/R and a color index 0..K-1.
- Every border position has a gate (side T/B/L/R + index). Gate color shown =
  (baseColor + clearedCount) mod K.   <- all gates shift +1 after every valid clear.
- Tap arrow: it travels along its direction.
    * any arrow in its path           -> BLOCKED (wrong, -1 life)
    * path clear, gate color != own  -> DENIED  (wrong, -1 life)
    * path clear, gate color == own  -> CLEARED (right, arrow leaves, clearedCount += 1)
- Win: no arrows left. Stuck: arrows left but no tap would be CLEARED.
"""
import json, random, sys

DIRS = {'U': (-1, 0), 'D': (1, 0), 'L': (0, -1), 'R': (0, 1)}
PALETTES = {
    1: ['mint'],
    2: ['coral', 'sky'],
    3: ['coral', 'mint', 'violet'],
    4: ['coral', 'sun', 'mint', 'sky'],
    5: ['coral', 'sun', 'mint', 'sky', 'violet'],
}

def exit_gate(N, r, c, d):
    if d == 'U': return ('T', c)
    if d == 'D': return ('B', c)
    if d == 'L': return ('L', r)
    return ('R', r)

def path_cells(N, r, c, d):
    dr, dc = DIRS[d]
    out = []
    rr, cc = r + dr, c + dc
    while 0 <= rr < N and 0 <= cc < N:
        out.append((rr, cc))
        rr += dr; cc += dc
    return out

# ---------------- engine (the part Cursor must port) ----------------
class Board:
    def __init__(self, level):
        self.N = level['size']; self.K = level['colors']
        self.gates = level['gates']
        self.arrows = level['arrows']            # list of dicts r,c,dir,color
        self.M = len(self.arrows)
        self.pos = {(a['r'], a['c']): i for i, a in enumerate(self.arrows)}
        self.blockers = []                       # bitmask of arrows on each path
        self.gate_base = []
        for a in self.arrows:
            m = 0
            for p in path_cells(self.N, a['r'], a['c'], a['dir']):
                if p in self.pos: m |= 1 << self.pos[p]
            self.blockers.append(m)
            side, idx = exit_gate(self.N, a['r'], a['c'], a['dir'])
            self.gate_base.append(self.gates[side][idx])
        self.full = (1 << self.M) - 1

    def gate_color(self, i, remaining):
        cleared = self.M - bin(remaining).count('1')
        return (self.gate_base[i] + cleared) % self.K

    def tap(self, i, remaining):
        """returns 'CLEARED' | 'BLOCKED' | 'DENIED' (does not mutate)"""
        if self.blockers[i] & remaining: return 'BLOCKED'
        if self.gate_color(i, remaining) != self.arrows[i]['color']: return 'DENIED'
        return 'CLEARED'

    def valid_moves(self, remaining):
        return [i for i in range(self.M) if remaining >> i & 1 and self.tap(i, remaining) == 'CLEARED']

# ---------------- solver / metrics ----------------
def solve(board, node_cap=400000):
    seen_dead = set(); nodes = [0]
    def dfs(rem, path):
        if rem == 0: return path
        if rem in seen_dead: return None
        nodes[0] += 1
        if nodes[0] > node_cap: return None
        for i in board.valid_moves(rem):
            r = dfs(rem & ~(1 << i), path + [i])
            if r is not None: return r
        seen_dead.add(rem)
        return None
    return dfs(board.full, [])

def random_playout_rate(board, trials, rng):
    wins = 0
    for _ in range(trials):
        rem = board.full
        while rem:
            mv = board.valid_moves(rem)
            if not mv: break
            rem &= ~(1 << rng.choice(mv))
        if rem == 0: wins += 1
    return wins / trials

# ---------------- generator (solvable by construction) ----------------
def generate(N, K, M, rng, cross_bias=1.5):
    gates = {s: [rng.randrange(K) for _ in range(N)] for s in 'TBLR'}
    occupied = {}; forbidden = set(); arrows = []
    for j in range(M):
        cands = []; weights = []
        for r in range(N):
            for c in range(N):
                if (r, c) in occupied or (r, c) in forbidden: continue
                for d in 'UDLR':
                    path = path_cells(N, r, c, d)
                    dep = sum(1 for p in path if p in occupied)
                    cands.append((r, c, d, path)); weights.append(1 + cross_bias * dep)
        if not cands: break
        r, c, d, path = rng.choices(cands, weights)[0]
        side, idx = exit_gate(N, r, c, d)
        color = (gates[side][idx] + j) % K      # correct color at the moment of its clear
        occupied[(r, c)] = j; forbidden.update(path)
        arrows.append({'r': r, 'c': c, 'dir': d, 'color': color})
    return {'size': N, 'colors': K, 'palette': PALETTES[K], 'lives': 3,
            'gates': gates, 'arrows': arrows}

def verify_by_replay(level):
    """arrows were appended in clearing order -> replaying 0..M-1 must be all CLEARED"""
    b = Board(level); rem = b.full
    for i in range(b.M):
        if b.tap(i, rem) != 'CLEARED': return False
        rem &= ~(1 << i)
    return True
