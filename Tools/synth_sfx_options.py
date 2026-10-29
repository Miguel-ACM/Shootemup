"""Four distinct procedural takes per cue for Miguel to choose between.

Run: python Tools/synth_sfx_options.py            render one take of every option for the picker
     python Tools/synth_sfx_options.py --apply picks.json
                                                  render four game variants of each chosen option
                                                  into Assets/Resources/Sounds/Synth

The styles differ in instrument, not just tuning:
  chip      dry square/pulse and stepped noise, the 8-bit arcade palette
  layered   the PSX-flavoured layered patches in synth_sfx.py (click, body, noise, small room)
  physical  noise, inharmonic metal and sub weight; closer to real impacts and mechanisms
  energy    FM, ring modulation, long sweeps and reverb; sci-fi plasma and shields
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np

import synth_sfx as S
from synth_sfx import (bp, chord, crackle, crush, drive, env, fit, fm, glide, hp, lp, mix, n_of, noise, osc, place,
                       ramp, room, sparkle, svf, time)

ROOT = S.ROOT
OPTIONS = ROOT / 'ArtSource/AudioReview/SynthOptions'
STYLES = ['chip', 'layered', 'physical', 'energy']


# ---------------------------------------------------------------- shared gestures

def chipify(x, rate=16000, bits=6):
    return crush(x, rate, bits)


def stepped_noise(seconds, rate0, rate1, seed=0):
    """Noise held at a falling sample rate: the classic console explosion and crunch."""
    r = np.random.default_rng(seed)
    rates = ramp(seconds, rate0, rate1)
    phase = np.cumsum(rates / S.SR).astype(int)
    values = r.uniform(-1, 1, phase[-1] + 2)
    return values[phase]


def sweep(seconds, f0, f1, rate, shape='square', pw=.5):
    return osc(glide(seconds, f0, f1, rate), shape, pw)


def arp(notes, step, shape='square', pw=.5, tail=.08, tau=.05):
    out = np.zeros(n_of(step * len(notes) + tail))
    for i, f in enumerate(notes):
        place(out, osc(np.full(n_of(step + tail), f), shape, pw) * env(step + tail, .001, tau), i * step)
    return out


def metal(f, seconds, tau, partials=(1, 2.76, 5.40, 8.93), seed=0):
    """Inharmonic struck-metal partials, each with its own decay."""
    r = np.random.default_rng(seed)
    out = np.zeros(n_of(seconds))
    for i, m in enumerate(partials):
        out += osc(np.full(n_of(seconds), f * m * r.uniform(.995, 1.005)), 'sine') * env(seconds, .0005, tau / (1 + i * .6)) / (1 + i * .5)
    return out


def thud(seconds, f0, f1, tau, rate=25):
    return osc(glide(seconds, f0, f1, rate), 'sine') * env(seconds, .001, tau)


def burst(seconds, lp0, lp1, tau, color='pink', rate=12, q=.8):
    return svf(noise(seconds, color), glide(seconds, lp0, lp1, rate), q=q) * env(seconds, .001, tau)


def ring(x, f):
    return x * osc(np.full(len(x), f), 'sine')


def siren(seconds, lo, hi, rate, shape='sine'):
    lfo = .5 + .5 * np.sin(2 * np.pi * rate * time(seconds) - np.pi / 2)
    return osc(lo + (hi - lo) * lfo, shape)


def swell(x, attack):
    return x * np.clip(np.arange(len(x)) / S.SR / attack, 0, 1) ** 2


# ---------------------------------------------------------------- per-cue options (layered comes from synth_sfx)

def k_of(v, amount=.03):
    return 1 + (v - 1.5) * amount


OPTION_DESIGNS = {}


def option(cue, style):
    def wrap(fn):
        OPTION_DESIGNS[(cue, style)] = fn
        return fn
    return wrap


# Base gun
@option('shot1', 'chip')
def _(v):
    k = k_of(v)
    return chipify(sweep(.07, 1700 * k, 520 * k, 55, 'square', .5) * env(.07, .0005, .025))


@option('shot1', 'physical')
def _(v):
    k = k_of(v, .05)
    crack = bp(noise(.05), 1500 * k, 6000) * env(.05, 0, .006)
    body = burst(.09, 4500, 600, .018, 'white', 40)
    sub = thud(.09, 190 * k, 70, .02, 50)
    mech = metal(2100 * k, .05, .008, seed=v)
    return mix(.09, (0, .6, crack), (0, .4, body), (0, .7, sub), (0, .15, mech))


@option('shot1', 'energy')
def _(v):
    k = k_of(v)
    d = .13
    laser = fm(glide(d, 2600 * k, 380 * k, 28), 1.5, 4 * env(d, 0, .03)) * env(d, .001, .04)
    tail = osc(glide(d, 1300 * k, 190, 25), 'saw') * env(d, .002, .03)
    return room(mix(d, (0, .55, laser), (0, .2, svf(tail, 2500, q=3))), .2, .3, .1)


# Hit on ordinary enemies
@option('enemyHit', 'chip')
def _(v):
    return chipify(lp(stepped_noise(.045, 14000, 3000, v), 7000) * env(.045, 0, .012), 12000, 5)


@option('enemyHit', 'physical')
def _(v):
    k = k_of(v, .06)
    return mix(.06, (0, .5, hp(noise(.02), 2500) * env(.02, 0, .004)), (0, .5, metal(1250 * k, .06, .02, seed=v)),
               (0, .4, thud(.04, 520 * k, 300, .01)))


@option('enemyHit', 'energy')
def _(v):
    k = k_of(v, .05)
    d = .08
    sizzle = fm(glide(d, 3400 * k, 1800 * k, 30), 1.41, 6 * env(d, 0, .01)) * env(d, .0005, .015)
    return room(mix(d, (0, .6, sizzle), (0, .25, crackle(d, 2000, .02, 3000, 9000, seed=v))), .1, .3, .08)


# Hit on bosses
@option('bossHit', 'chip')
def _(v):
    k = k_of(v)
    tone = sweep(.09, 320 * k, 150 * k, 30, 'square', .25) * env(.09, .0005, .03)
    return chipify(mix(.09, (0, .5, tone), (0, .5, stepped_noise(.06, 8000, 2000, v) * env(.06, 0, .015))), 12000, 5)


@option('bossHit', 'physical')
def _(v):
    k = k_of(v, .04)
    return room(mix(.25, (0, .5, metal(340 * k, .25, .09, (1, 1.58, 2.33, 3.71, 4.9), seed=v)),
                    (0, .7, thud(.15, 130 * k, 50, .04)), (0, .4, hp(noise(.03), 1800) * env(.03, 0, .006))), .3, .4, .12)


@option('bossHit', 'energy')
def _(v):
    k = k_of(v, .03)
    d = .22
    shield = fm(glide(d, 900 * k, 1500 * k, 12), 2.5, 3 * env(d, 0, .05)) * env(d, .001, .06)
    return room(mix(d, (0, .5, shield), (0, .3, ring(burst(.06, 6000, 1500, .015, 'white'), 310))), .4, .45, .2)


# Player loses a segment
@option('hurt1', 'chip')
def _(v):
    d = .4
    k = k_of(v, .02)
    tone = osc(np.repeat([880 * k, 660 * k, 440 * k, 330 * k, 220 * k], n_of(.06)), 'square', .25)
    tone = fit(tone, d) * env(d, .001, .15, hold=.15)
    return chipify(mix(d, (0, .5, tone), (0, .45, stepped_noise(.15, 9000, 1500, v) * env(.15, 0, .05))))


@option('hurt1', 'physical')
def _(v):
    d = .5
    k = k_of(v, .03)
    crash = drive(burst(d, 7000, 900, .09, 'white', 9), 3.5)
    return room(mix(d, (0, .55, crash), (0, .4, metal(480 * k, .4, .12, seed=v)), (0, .8, thud(.3, 120, 38, .09, 18)),
                    (.04, .35, crackle(.4, 400, .1, 1200, 6000, seed=v))), .3, .4, .14)


@option('hurt1', 'energy')
def _(v):
    d = .55
    k = k_of(v, .02)
    shock = ring(sweep(.35, 1800 * k, 300, 9, 'saw'), 73) * env(.35, .001, .12)
    alarm = fit(osc(np.repeat([1500 * k, 1000 * k] * 2, n_of(.06)), 'sine'), .24) * env(.24, .002, .2)
    return room(mix(d, (0, .4, svf(shock, 3000, q=2)), (0, .4, crackle(.3, 1500, .08, 2500, 9000, seed=v)),
                    (.12, .25, alarm)), .5, .5, .22)


# Small enemy explosion
@option('enemyExplosion1', 'chip')
def _(v):
    d = .4
    return chipify(lp(stepped_noise(d, 9000, 600, v), 6000) * env(d, .001, .1), 14000, 5)


@option('enemyExplosion1', 'physical')
def _(v):
    d = .65
    k = k_of(v, .05)
    blast = drive(burst(d, 5000 * k, 160, .11, 'pink', 10), 2.8)
    return room(mix(d, (0, .55, blast), (0, .9, thud(d, 110 * k, 34, .1, 18)), (.02, .45, crackle(d, 260, .2, 800, 5000, seed=v)),
                    (0, .3, lp(noise(d, 'brown'), 300) * env(d, .01, .2))), .45, .45, .18)


@option('enemyExplosion1', 'energy')
def _(v):
    d = .6
    k = k_of(v, .05)
    plasma = fm(glide(d, 900 * k, 60, 9), .5, 6 * env(d, 0, .08)) * env(d, .001, .1)
    pop = ring(burst(.15, 9000, 800, .04, 'white', 20), 220 * k)
    return room(mix(d, (0, .55, plasma), (0, .4, pop), (0, .5, thud(.3, 150, 45, .06))), .6, .5, .28)


# Boss phase change
@option('explosionLarge', 'chip')
def _(v):
    d = 1.2
    k = k_of(v, .02)
    fall = fit(osc(np.repeat([660 * k, 523 * k, 440 * k, 349 * k, 294 * k, 220 * k], n_of(.07)), 'square', .5), .42) * env(.42, .001, .3, hold=.3)
    return chipify(mix(d, (0, .55, lp(stepped_noise(d, 8000, 300, v), 5000) * env(d, .001, .3)), (0, .3, fall)), 14000, 5)


@option('explosionLarge', 'physical')
def _(v):
    d = 1.6
    k = k_of(v, .04)
    out = np.zeros(n_of(d))
    for i, at in enumerate((0, .11, .27)):
        place(out, drive(burst(1.2, 4500 * k / (1 + i * .3), 120, .22, 'pink', 6), 2.5) * (1 - i * .2), at)
    return room(mix(d, (0, .6, out), (0, 1, thud(d, 85 * k, 28, .35, 6)), (.05, .45, crackle(d, 220, .5, 600, 5000, seed=v)),
                    (0, .5, lp(noise(d, 'brown'), 200) * env(d, .03, .5))), .9, .6, .25)


@option('explosionLarge', 'energy')
def _(v):
    d = 1.6
    k = k_of(v, .03)
    implode = swell(fm(ramp(.35, 200 * k, 1800 * k, 2), 1.5, 3), .35) * .8
    blast = fm(glide(1.2, 700 * k, 40, 5), .5, 8 * env(1.2, 0, .2)) * env(1.2, .001, .3)
    shimmer = sparkle(1.2, 60, .4, 2000, 3, seed=v)
    return room(mix(d, (0, .35, implode), (.35, .6, blast), (.35, .2, shimmer), (.35, .7, thud(.6, 100, 30, .15))), 1.0, .6, .35)


# Boss destroyed
@option('bossDeath', 'chip')
def _(v):
    d = 3.0
    out = np.zeros(n_of(d))
    for i in range(6):
        place(out, lp(stepped_noise(.6, 9000 - i * 900, 300, v + i), 5000) * env(.6, .001, .15) * (1 - i * .08), i * .16)
    melody = arp([1047, 784, 659, 523, 392, 330, 262, 196], .1, 'square', .5, .3, .12)
    return chipify(mix(d, (0, .5, out), (.95, .3, melody), (.95, .4, lp(stepped_noise(1.6, 5000, 150, v), 3000) * env(1.6, .001, .5))), 14000, 5)


@option('bossDeath', 'physical')
def _(v):
    d = 3.6
    big = OPTION_DESIGNS[('explosionLarge', 'physical')]
    out = mix(d, (0, .9, big(v)), (.35, .7, big((v + 1) % 4)), (.8, .8, big((v + 2) % 4)))
    groan = svf(osc(ramp(2.2, 160, 40, .7), 'saw'), 600, q=4) * env(2.2, .2, .8)
    return room(mix(d, (0, 1, out), (.5, .3, groan), (.6, .45, crackle(3.0, 120, 1.2, 400, 4000, seed=v))), 1.2, .7, .32, damp=3000, predelay=.025)


@option('bossDeath', 'energy')
def _(v):
    d = 3.6
    k = k_of(v, .02)
    charge = swell(fm(ramp(.9, 120 * k, 2400 * k, 2.5), 1.01, 2 + 3 * np.linspace(0, 1, n_of(.9))), .9)
    nova = fm(glide(2.4, 1200 * k, 30, 2.5), .5, 10 * env(2.4, 0, .4)) * env(2.4, .001, .7)
    bells = sparkle(2.4, 50, .9, 1500, 4, seed=v)
    return room(mix(d, (0, .3, charge), (.9, .6, nova), (.9, .25, bells), (.9, .8, thud(1.2, 90, 22, .4, 3))), 1.3, .7, .4, damp=5000)


# Player destroyed
@option('playerDead', 'chip')
def _(v):
    d = 2.2
    fall = chipify(osc(ramp(1.4, 1200, 60, .6), 'square', .5) * env(1.4, .001, .6, hold=.6))
    melody = arp([523, 494, 466, 440, 415, 392], .16, 'square', .25, .2, .1)
    return mix(d, (0, .35, fall), (0, .5, chipify(lp(stepped_noise(1.2, 9000, 200, v), 5000) * env(1.2, .001, .35))), (1.0, .35, chipify(melody)))


@option('playerDead', 'physical')
def _(v):
    d = 2.8
    out = mix(d, (0, .7, OPTION_DESIGNS[('hurt1', 'physical')](v)), (.15, .9, OPTION_DESIGNS[('explosionLarge', 'physical')](v)))
    return room(mix(d, (0, 1, out), (.4, .4, crackle(2.2, 160, .9, 400, 4000, seed=v))), 1.0, .6, .3, damp=3500)


@option('playerDead', 'energy')
def _(v):
    d = 2.8
    whine = svf(osc(ramp(2.0, 2400, 40, .5), 'saw') + osc(ramp(2.0, 2420, 41, .5), 'saw'), ramp(2.0, 6000, 200), q=3) * env(2.0, .005, .9)
    collapse = fm(glide(1.6, 400, 30, 2.5), .5, 6 * env(1.6, 0, .4)) * env(1.6, .01, .6)
    return room(mix(d, (0, .3, ring(whine, 31)), (.1, .6, collapse), (.1, .3, crackle(1.5, 600, .4, 2000, 9000, seed=v))), 1.1, .65, .4)


# Screen clear / shield burst
@option('blankbullet', 'chip')
def _(v):
    d = 1.0
    up = arp([262, 330, 392, 523, 659, 784, 1047, 1319], .035, 'square', .5, .2, .08)
    return chipify(mix(d, (0, .4, up), (0, .45, lp(stepped_noise(.8, 2000, 12000, v), 9000) * env(.8, .05, .25, hold=.1))))


@option('blankbullet', 'physical')
def _(v):
    d = 1.3
    blast = svf(noise(d, 'pink'), np.concatenate([ramp(.06, 300, 9000, .7), ramp(d - .06, 9000, 250, .4)])[:n_of(d)], q=.9) * env(d, .004, .28)
    return room(mix(d, (0, .7, drive(blast, 2)), (0, 1, thud(d, 120, 26, .3, 6)), (0, .25, metal(240, .9, .4, seed=v))), .9, .6, .3)


@option('blankbullet', 'energy')
def _(v):
    d = 1.5
    k = k_of(v, .02)
    sweep_ = fm(ramp(.25, 300 * k, 3000 * k, .6), 2.0, 4 * env(.25, 0, .1)) * env(.25, .002, .1, hold=.1)
    wave = fm(np.full(n_of(1.2), 523 * k), 3.01, 5 * env(1.2, 0, .15)) * env(1.2, .002, .35)
    return room(mix(d, (0, .4, sweep_), (.2, .5, wave), (.2, .25, sparkle(1.2, 80, .4, 2500, 3, seed=v)),
                    (.2, .7, thud(.5, 110, 35, .12))), 1.1, .65, .4, damp=7000)


# Boss incoming
@option('bossWarning', 'chip')
def _(v):
    d = 2.6
    beep = np.zeros(n_of(d))
    for i in range(8):
        f = 880 if i % 2 == 0 else 659
        place(beep, osc(np.full(n_of(.22), f), 'square', .5) * env(.22, .002, .2, hold=.15), i * .3)
    return chipify(mix(d, (0, .4, beep), (0, .3, osc(np.full(n_of(d), 55), 'square', .5) * env(d, .3, .6, hold=1.8))))


@option('bossWarning', 'physical')
def _(v):
    d = 3.0
    horn = np.zeros(n_of(d))
    for i in range(3):
        blast_ = sum(osc(np.full(n_of(.7), f), 'saw') for f in (98, 98.6, 147, 196.4)) * env(.7, .04, .3, hold=.45)
        place(horn, svf(blast_, 900, q=1.2), i * .85)
    return room(mix(d, (0, .5, drive(horn, 2)), (0, .35, lp(noise(d, 'brown'), 150) * env(d, .5, .8, hold=1.5))), 1.2, .65, .3, damp=2500)


@option('bossWarning', 'energy')
def _(v):
    d = 2.8
    wail = siren(2.6, 420, 980, 1.25) * env(2.6, .1, .4, hold=2.1)
    pulse = .6 + .4 * np.tanh(5 * np.sin(2 * np.pi * 8 * time(2.6)))
    return room(mix(d, (0, .4, ring(wail, 3) * .4 + wail * pulse * .6), (0, .35, svf(osc(np.full(n_of(d), 41), 'saw'), ramp(d, 150, 1200), q=3) * env(d, .4, .5, hold=1.8))), 1.0, .6, .3)


# Coin
@option('coin', 'chip')
def _(v):
    return chipify(mix(.25, (0, .4, osc(np.full(n_of(.06), 1319), 'square', .5) * env(.06, .001, .5)),
                       (.06, .4, osc(np.full(n_of(.19), 1760), 'square', .5) * env(.19, .001, .06, hold=.03))))


@option('coin', 'physical')
def _(v):
    k = k_of(v, .04)
    return room(mix(.35, (0, .5, metal(2350 * k, .3, .12, (1, 2.32, 4.25, 6.63), seed=v)),
                    (.045, .35, metal(3100 * k, .25, .08, (1, 2.32, 4.25), seed=v + 7)), (0, .2, hp(noise(.01), 4000) * env(.01, 0, .002))), .2, .35, .12)


@option('coin', 'energy')
def _(v):
    k = k_of(v, .03)
    gliss = fm(ramp(.12, 1500 * k, 3000 * k), 2.0, 1.5) * env(.12, .002, .1, hold=.06)
    return room(mix(.4, (0, .4, gliss), (.08, .25, sparkle(.3, 60, .15, 3500, 2, seed=v))), .4, .5, .25)


# Energy cell
@option('mana', 'chip')
def _(v):
    return chipify(arp([784, 988, 1175, 1568], .035, 'square', .25, .12, .05))


@option('mana', 'physical')
def _(v):
    k = k_of(v, .04)
    bubble = osc(ramp(.12, 400 * k, 1400 * k, .6), 'sine') * env(.12, .002, .05)
    return room(mix(.3, (0, .5, bubble), (.06, .35, metal(1800 * k, .2, .07, (1, 2.0, 3.01), seed=v))), .2, .3, .1)


@option('mana', 'energy')
def _(v):
    k = k_of(v, .03)
    charge = fm(ramp(.3, 300 * k, 1600 * k, 1.5), 1.0, 2 * env(.3, .2, .1)) * env(.3, .05, .1, hold=.15)
    return room(mix(.45, (0, .45, charge), (.25, .3, fm(np.full(n_of(.2), 1600 * k), 3, env(.2, 0, .03)) * env(.2, .001, .06))), .5, .5, .25)


# Item collected
@option('itemGet', 'chip')
def _(v):
    fan = arp([523, 659, 784, 1047, 784, 1047, 1319], .06, 'square', .5, .3, .08)
    return chipify(fan)


@option('itemGet', 'physical')
def _(v):
    out = np.zeros(n_of(1.1))
    for i, f in enumerate((587, 880, 1175)):
        place(out, metal(f, .9, .35, (1, 2.0, 3.0, 4.07), seed=v + i), i * .09)
    return room(out, .6, .55, .3)


@option('itemGet', 'energy')
def _(v):
    d = 1.3
    rise = swell(fm(ramp(.4, 250, 1050, 1.6), 2.0, 2), .4)
    chord_ = sum(fm(np.full(n_of(.9), f), 2.0, 1.4 * env(.9, 0, .2)) for f in (1047, 1319, 1568)) * env(.9, .002, .3)
    return room(mix(d, (0, .3, rise), (.4, .35, chord_), (.4, .25, sparkle(.9, 60, .35, 2600, 2.5, seed=v))), .9, .6, .35)


# Purchase / confirm
@option('uiConfirm', 'chip')
def _(v):
    return chipify(arp([988, 1319], .05, 'square', .5, .12, .05))


@option('uiConfirm', 'physical')
def _(v):
    click = hp(noise(.008), 2000) * env(.008, 0, .0015)
    register = mix(.4, (0, .4, click), (.03, .4, metal(2637, .3, .1, (1, 2.4, 3.9), seed=v)), (.09, .35, metal(3520, .3, .12, (1, 2.4, 3.9), seed=v + 3)))
    return room(register, .25, .35, .12)


@option('uiConfirm', 'energy')
def _(v):
    up = fm(ramp(.14, 600, 1400, .5), 1.0, 1.5 * env(.14, 0, .1)) * env(.14, .002, .1, hold=.05)
    return room(mix(.3, (0, .5, up), (.1, .3, fm(np.full(n_of(.2), 2800), 2, env(.2, 0, .04)) * env(.2, .001, .05))), .3, .45, .2)


# Skip
@option('uiSkip', 'chip')
def _(v):
    return chipify(arp([784, 523], .06, 'square', .25, .1, .05))


@option('uiSkip', 'physical')
def _(v):
    return mix(.2, (0, .5, bp(noise(.12), 600, 2500) * env(.12, .004, .03)), (0, .4, thud(.08, 300, 140, .02)))


@option('uiSkip', 'energy')
def _(v):
    down = fm(ramp(.16, 1200, 420, .6), 1.0, 1.5 * env(.16, 0, .1)) * env(.16, .002, .06)
    return room(mix(.25, (0, .55, down)), .3, .4, .18)


# Heal
@option('heal', 'chip')
def _(v):
    return chipify(arp([523, 659, 784, 1047, 1319, 1568], .045, 'tri', .5, .25, .08))


@option('heal', 'physical')
def _(v):
    pour = bp(noise(.6, 'pink'), 600, 2600) * env(.6, .08, .2, hold=.2) * (.6 + .4 * np.sin(2 * np.pi * 11 * time(.6)))
    chime = mix(.7, *[(i * .08, .3, metal(f, .4, .15, (1, 3.0, 5.0), seed=v + i)) for i, f in enumerate((784, 988, 1175))])
    return room(mix(.8, (0, .3, pour), (.05, .6, chime)), .5, .5, .25)


@option('heal', 'energy')
def _(v):
    rise = swell(fm(ramp(.6, 300, 900, 1.2), 1.5, 1.2) * (1 + .4 * np.sin(2 * np.pi * 7 * time(.6))), .3) * env(.6, 0, .4, hold=.4)
    return room(mix(.9, (0, .35, rise), (.2, .3, sparkle(.6, 70, .3, 2000, 3, seed=v))), .8, .6, .35)


# Time stop
@option('timeStop', 'chip')
def _(v):
    halt = chipify(osc(np.repeat([1568, 1319, 1047, 784, 523, 262], n_of(.07)), 'square', .5))
    return mix(.7, (0, .4, fit(halt, .45) * env(.45, .001, .5, hold=.3)), (.42, .4, chipify(osc(np.full(n_of(.25), 131), 'square', .5) * env(.25, .001, .1))))


@option('timeStop', 'physical')
def _(v):
    clock = mix(.9, *[(i * .12, .4, metal(2200 - i * 300, .15, .03, (1, 2.6), seed=v)) for i in range(4)])
    clunk = thud(.4, 140, 50, .1) + .4 * metal(330, .4, .12, seed=v)
    return room(mix(1.0, (0, .6, clock), (.48, .6, clunk)), .6, .5, .25)


@option('timeStop', 'energy')
def _(v):
    stop = np.exp(-time(1.0) * 3.5)
    freeze = fm(1500 * stop + 40, 1.5, 3 * stop) * env(1.0, .002, .5)
    return room(mix(1.2, (0, .45, freeze), (0, .3, sparkle(.8, 80, .3, 3000, 2, seed=v)), (0, .3, hp(noise(.3), 5000) * env(.3, .002, .08))), 1.0, .7, .45)


# Power down (time stop ends, overdrive ends)
@option('powerdown', 'chip')
def _(v):
    return chipify(osc(ramp(.4, 900, 90, .6), 'square', .5) * env(.4, .001, .25, hold=.15))


@option('powerdown', 'physical')
def _(v):
    spin = bp(noise(.6), 300, 2000) * (.5 + .5 * np.sin(2 * np.pi * ramp(.6, 30, 4).cumsum() / S.SR)) * env(.6, .005, .3)
    return mix(.65, (0, .6, spin), (.4, .4, thud(.2, 160, 60, .05)))


@option('powerdown', 'energy')
def _(v):
    return room(mix(.6, (0, .5, ring(fm(ramp(.55, 1200, 80, .5), .5, 3), 45) * env(.55, .003, .25))), .4, .45, .2)


# Overdrive
@option('overdrive', 'chip')
def _(v):
    return chipify(osc(ramp(.5, 110, 880, 1.5), 'square', .25) * env(.5, .001, .3, hold=.35))


@option('overdrive', 'physical')
def _(v):
    rev = svf(drive(osc(ramp(.65, 45, 140, 1.2), 'saw') * 2, 3) + bp(noise(.65), 200, 1200) * .4, ramp(.65, 400, 2600), q=1.5)
    return room(mix(.7, (0, .7, rev * env(.65, .02, .3, hold=.4))), .3, .4, .12)


@option('overdrive', 'energy')
def _(v):
    charge = fm(ramp(.6, 150, 1100, 1.4), 1.01, 1 + 4 * np.linspace(0, 1, n_of(.6))) * env(.6, .02, .3, hold=.4)
    return room(mix(.75, (0, .45, charge), (.58, .4, burst(.1, 9000, 1000, .03, 'white'))), .5, .5, .25)


# Roll / dash
@option('roll', 'chip')
def _(v):
    return chipify(lp(stepped_noise(.3, 3000, 14000, v), 8000) * np.sin(np.pi * np.linspace(0, 1, n_of(.3))))


@option('roll', 'physical')
def _(v):
    d = .45
    air = svf(noise(d, 'pink'), ramp(d, 300, 1800, .8), q=.9) * np.sin(np.pi * np.linspace(0, 1, n_of(d))) ** 2
    return mix(d, (0, 1, air))


@option('roll', 'energy')
def _(v):
    d = .4
    return room(mix(d, (0, .45, fm(ramp(d, 400, 1600, .7), .5, 2) * np.sin(np.pi * np.linspace(0, 1, n_of(d))))), .3, .4, .2)


# Ready
@option('ready', 'chip')
def _(v):
    return chipify(arp([1319, 1976], .07, 'square', .5, .12, .05))


@option('ready', 'physical')
def _(v):
    latch = hp(noise(.01), 2500) * env(.01, 0, .002)
    return room(mix(.35, (0, .5, latch), (.005, .5, metal(1600, .3, .1, (1, 2.76, 5.4), seed=v)), (.06, .4, latch)), .2, .3, .1)


@option('ready', 'energy')
def _(v):
    return room(mix(.5, (0, .5, fm(np.full(n_of(.4), 1760), 1.0, 3 * env(.4, 0, .06)) * env(.4, .001, .12) * (1 + .3 * np.sin(2 * np.pi * 12 * time(.4))))), .5, .5, .25)


# Shatter
@option('shatter', 'chip')
def _(v):
    return chipify(mix(.4, (0, .4, hp(stepped_noise(.35, 16000, 6000, v), 2000) * env(.35, 0, .08)),
                       (0, .3, arp([2093, 1568, 2637, 1760], .03, 'square', .25, .05, .02))))


@option('shatter', 'energy')
def _(v):
    d = .7
    burst_ = ring(burst(.3, 10000, 2000, .05, 'white'), 470)
    shards = sparkle(.6, 120, .15, 3000, 3, seed=v)
    return room(mix(d, (0, .45, burst_), (0, .5, shards), (0, .4, fm(glide(.3, 1200, 200, 15), 2.5, 4 * env(.3, 0, .05)) * env(.3, .001, .06))), .6, .55, .3)


@option('shatter', 'physical')
def _(v):
    d = .7
    out = np.zeros(n_of(d))
    r = np.random.default_rng(v)
    for _i in range(22):
        place(out, metal(r.uniform(1500, 6000), .25, r.uniform(.02, .07), (1, 2.3, 3.7), seed=int(r.integers(1000))) * r.uniform(.2, 1), r.exponential(.07))
    return room(mix(d, (0, .4, out), (0, .45, hp(noise(.08), 1200) * env(.08, 0, .02)), (0, .4, thud(.1, 300, 120, .02))), .4, .45, .2, damp=9000)


# Capacitor discharge
@option('capacitor', 'chip')
def _(v):
    return chipify(mix(.35, (0, .4, osc(ramp(.2, 200, 1600, 1.5), 'square', .25) * env(.2, .001, .3)),
                       (.2, .45, stepped_noise(.12, 14000, 3000, v) * env(.12, 0, .03))))


@option('capacitor', 'physical')
def _(v):
    hum = bp(osc(np.full(n_of(.25), 60), 'saw') + .3 * noise(.25), 100, 1500) * env(.25, .05, .2, hold=.15)
    snap = hp(noise(.03), 1500) * env(.03, 0, .006)
    return mix(.4, (0, .5, hum), (.23, .6, snap), (.23, .4, thud(.1, 200, 70, .025)))


@option('capacitor', 'energy')
def _(v):
    whine = fm(ramp(.25, 800, 5000, 1.6), 1.0, 2) * env(.25, .01, .2, hold=.2)
    discharge = ring(sweep(.18, 4000, 150, 20, 'square'), 90) * env(.18, .001, .05)
    return room(mix(.5, (0, .3, whine), (.25, .45, discharge), (.25, .35, crackle(.2, 1200, .06, 3000, 10000, seed=v))), .4, .45, .2)


# Fan
@option('fan', 'chip')
def _(v):
    gate = (np.sin(2 * np.pi * ramp(.6, 10, 26).cumsum() / S.SR) > 0) * .6 + .4
    return chipify(lp(stepped_noise(.6, 4000, 9000, v), 6000) * gate * env(.6, .05, .2, hold=.3))


@option('fan', 'energy')
def _(v):
    d = .7
    vortex = fm(ramp(d, 200, 600), .5, 4 * (.5 + .5 * np.sin(2 * np.pi * 14 * time(d)))) * env(d, .1, .2, hold=.35)
    return room(mix(d, (0, .4, vortex), (0, .3, svf(noise(d), ramp(d, 2000, 6000), q=4, mode='bp') * env(d, .1, .2, hold=.35))), .5, .5, .25)


@option('fan', 'physical')
def _(v):
    d = .7
    blades = .55 + .45 * np.abs(np.sin(2 * np.pi * ramp(d, 12, 30).cumsum() / S.SR))
    motor = osc(ramp(d, 90, 180), 'saw') * .15
    return mix(d, (0, .8, (bp(noise(d, 'pink'), 400, 3000) + motor) * blades * env(d, .08, .2, hold=.35)))


# Frost
@option('frost', 'chip')
def _(v):
    return chipify(mix(.35, (0, .35, arp([3136, 2637, 2349, 2093], .025, 'tri', .5, .1, .03)),
                       (0, .3, hp(stepped_noise(.3, 16000, 9000, v), 4000) * env(.3, 0, .07))))


@option('frost', 'physical')
def _(v):
    d = .5
    crunch = hp(crackle(d, 1500, .15, 1500, 9000, seed=v), 1000)
    return room(mix(d, (0, .6, crunch), (0, .25, metal(3800, .3, .08, (1, 2.1, 3.3), seed=v))), .3, .4, .15, damp=9000)


@option('frost', 'energy')
def _(v):
    d = .6
    return room(mix(d, (0, .4, fm(glide(.5, 4200, 2600, 6), 3.5, 2 * env(.5, 0, .1)) * env(.5, .002, .15)),
                    (0, .3, sparkle(.5, 90, .2, 4000, 1.8, seed=v))), .7, .6, .35, damp=10000)


# Decoy
@option('decoy', 'chip')
def _(v):
    return chipify(arp([659, 784, 659, 784], .06, 'square', .25, .1, .05))


@option('decoy', 'physical')
def _(v):
    d = .6
    breath = bp(noise(d, 'pink'), 700, 2200) * np.sin(np.pi * np.linspace(0, 1, n_of(d))) ** 2
    return room(mix(d, (0, .7, breath), (0, .25, osc(np.full(n_of(d), 220), 'tri') * np.sin(np.pi * np.linspace(0, 1, n_of(d))))), .6, .6, .3)


@option('decoy', 'energy')
def _(v):
    d = .8
    f = 600 * (1 + .03 * np.sin(2 * np.pi * 9 * time(d)))
    return room(mix(d, (0, .4, ring(fm(f, 1.5, 1.5), 7) * np.sin(np.pi * np.linspace(0, 1, n_of(d))))), .9, .65, .4)


# Popcorn
@option('popcorn', 'chip')
def _(v):
    k = k_of(v, .1)
    return chipify(sweep(.05, 1400 * k, 500 * k, 70, 'square', .5) * env(.05, 0, .015))


@option('popcorn', 'physical')
def _(v):
    k = k_of(v, .1)
    return mix(.1, (0, .6, bp(noise(.02), 900 * k, 4000) * env(.02, 0, .003)), (0, .4, thud(.06, 700 * k, 350, .01)))


@option('popcorn', 'energy')
def _(v):
    k = k_of(v, .1)
    return room(fm(glide(.12, 600 * k, 1800 * k, 30), 2, 3 * env(.12, 0, .02)) * env(.12, .0005, .025), .2, .35, .12)


# Static
@option('static', 'chip')
def _(v):
    stutter = (np.sin(2 * np.pi * 37 * time(.25)) > 0) * .7 + .3
    return chipify(stepped_noise(.25, 16000, 16000, v) * env(.25, 0, .07) * stutter)


@option('static', 'physical')
def _(v):
    return mix(.3, (0, .7, crackle(.3, 2500, .1, 1500, 9000, seed=v)), (0, .3, bp(osc(np.full(n_of(.3), 50), 'square', .5), 100, 800) * env(.3, .002, .1)))


@option('static', 'energy')
def _(v):
    d = .3
    bolt = fit(np.concatenate([fm(np.full(n_of(.03), f), 1.41, 5) for f in np.random.default_rng(v).uniform(1500, 6000, 10)]), d)
    return room(mix(d, (0, .5, bolt * env(d, 0, .08))), .3, .4, .2)


# ---------------------------------------------------------------- rendering

LABELS = {'chip': 'Chip', 'layered': 'Layered', 'physical': 'Physical', 'energy': 'Energy'}


def design(cue, style):
    if style == 'layered':
        return S.CUES[cue][0]
    return OPTION_DESIGNS[(cue, style)]


def level_target(cue):
    original = S.CUES[cue][1]
    source, level = (original, 1.0) if isinstance(original, str) else original
    return S.loudness(S.read_wav(ROOT / 'Assets/Resources' / (source + '.wav'))) * level


def take(cue, style, v):
    S.rng = np.random.default_rng(int.from_bytes(hashlib.sha256(f'{cue}:{style}:{v}:opt-v1'.encode()).digest()[:8], 'little'))
    x = S.finish(design(cue, style)(v))
    return x * min(level_target(cue) / S.loudness(x), .98 / np.max(np.abs(x)))


def render_options():
    audio = OPTIONS / 'audio'
    audio.mkdir(parents=True, exist_ok=True)
    rows = []
    for cue, (_, original, label, group) in S.CUES.items():
        files = {}
        for style in STYLES:
            name = f'{cue}_{style}.wav'
            S.write_wav(audio / name, take(cue, style, 0))
            files[style] = name
        current = None
        if isinstance(original, str):
            current = f'current_{cue}.wav'
            S.write_wav(audio / current, S.read_wav(ROOT / 'Assets/Resources' / (original + '.wav')))
        rows.append(dict(id=cue, label=label, group=group, current=current, options=files, repeat=S.REPEAT.get(cue)))
        print(cue)
    (OPTIONS / 'cues.json').write_text(json.dumps(rows, indent=1), encoding='utf-8')


def apply(picks):
    """picks: {cue: style | 'current'}. Chosen styles replace the game bank; 'current' removes the synth cue."""
    for cue, style in picks.items():
        for old in S.OUT.glob(f'{cue}_[0-9][0-9].wav*'):
            old.unlink()
        if style not in STYLES:
            continue
        for v in range(S.VARIANTS):
            path = S.OUT / f'{cue}_{v + 1:02}.wav'
            S.write_wav(path, take(cue, style, v))
            S.meta(path)
        print(cue, style)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--apply', help='JSON file of {cue: style}')
    args = parser.parse_args()
    if args.apply:
        apply(json.loads(Path(args.apply).read_text(encoding='utf-8')))
    else:
        render_options()
