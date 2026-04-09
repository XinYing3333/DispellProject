INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)
#layout:layout1
{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2 #audio:default
{ lang == "zh":
    紅綠燈居然睡着了...
- else:
    The traffic light is actually asleep…
}
{ lang == "zh":
    找個東西把他叫醒吧。
- else:
    Let's find something to wake it up.
}

-> END
