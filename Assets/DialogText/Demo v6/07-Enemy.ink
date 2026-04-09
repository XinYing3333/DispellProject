INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)
#layout:layout1
{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:default
{ lang == "zh":
    人類注意！那是<color=\#FF2424>有攻擊性</color>的暴走念頭。
- else:
    Heads up! That’s an <color=\#FF2424>Aggressive</color> thought.
}
{ lang == "zh":
    需要把他<color=\#FF2424>擊暈</color>才能吸收。
- else:
    You'll need to <color=\#FF2424>STUN</color> it before we can absorb it.
}

-> END
