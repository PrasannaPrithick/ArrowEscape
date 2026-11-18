import random, json, level_tools as lt

# id: (N, K, M, (minWin, maxWin), note)
PLAN = {
 1:(4,1,5,(1,1),'Tutorial: tap an arrow, it leaves'),
 2:(4,1,6,(1,1),'Tutorial: a blocked arrow costs a heart'),
 3:(4,1,8,(1,1),'Tutorial: clear blockers first (chains)'),
 4:(5,2,8,(.75,1),'Intro: 2 colors, gates change after each clear'),
 5:(5,2,9,(.80,1),'Breather'),
 6:(5,2,10,(.60,.92),''),
 7:(5,2,11,(.50,.85),''),
 8:(5,3,10,(.45,.80),'Intro: 3 colors'),
 9:(6,3,12,(.40,.75),'Intro: 6x6 board'),
 10:(6,3,12,(.50,.85),'Breather'),
 11:(6,3,14,(.30,.65),''),
 12:(6,3,15,(.25,.60),''),
 13:(6,4,14,(.30,.60),'Intro: 4 colors'),
 14:(6,4,15,(.25,.55),''),
 15:(6,4,14,(.35,.70),'Breather'),
 16:(7,4,17,(.20,.50),'Intro: 7x7 board'),
 17:(7,4,19,(.15,.45),''),
 18:(7,4,20,(.12,.40),''),
 19:(7,5,18,(.15,.45),'Intro: 5 colors'),
 20:(7,5,18,(.25,.55),'Breather (end of picture 1)'),
 21:(7,5,21,(.08,.35),''),
 22:(8,5,22,(.08,.30),'Intro: 8x8 board'),
 23:(8,5,24,(.05,.25),''),
 24:(8,5,25,(.04,.22),''),
 25:(8,5,26,(.02,.18),'Finale of this set'),
}

def initial_stats(b):
    valid = b.valid_moves(b.full)
    blocked = sum(1 for i in range(b.M) if b.blockers[i])
    return len(valid), blocked

levels = []
for lid, (N,K,M,(lo,hi),note) in PLAN.items():
    chosen=None
    for attempt in range(1, 6000):
        seed = lid*100000 + attempt
        rng = random.Random(seed)
        lvl = lt.generate(N,K,M, rng, cross_bias=2.0 if K==1 else 1.5)
        if len(lvl['arrows']) != M: continue
        b = lt.Board(lvl)
        v, blocked = initial_stats(b)
        if lid==1 and (v<3 or blocked<1): continue
        if lid in (2,3) and blocked < (3 if lid==2 else 5): continue
        if K>1 and v<2: continue                      # always give the player a choice
        rate = lt.random_playout_rate(b, 500, rng)
        if not (lo <= rate <= hi): continue
        chosen=(lvl,b,seed,attempt); break
    assert chosen, f"no level for {lid}"
    lvl,b,seed,attempt = chosen
    assert lt.verify_by_replay(lvl)
    sol = lt.solve(b)                                  # independent DFS verification
    assert sol is not None, f"DFS could not solve {lid}"
    rate = lt.random_playout_rate(b, 4000, random.Random(seed+1))
    v, blocked = initial_stats(b)
    # re-order arrows so index == clearing order of the stored solution (solution = 0..M-1)
    order = list(range(b.M))
    lvl['id']=lid; lvl['note']=note
    lvl['meta']={'seed':seed,'arrows':M,'initialValidMoves':v,'initiallyBlockedArrows':blocked,
                 'randomPlayWinRate':round(rate,3)}
    lvl['solution']=order        # arrows[] is stored in a valid clearing order
    levels.append(lvl)
    print(f"L{lid:>2} {N}x{N} K={K} arrows={M:>2} validAtStart={v} blocked={blocked:>2} randomWin={rate:.2f}")

json.dump({'version':1,'game':'Hue Gates','levels':levels}, open('levels_1_25.json','w'), indent=1)
print('saved', len(levels))
