INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)
#layout:layout1
{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:none
{ lang == "zh":
    回收的<color=\#FF2424>念頭</color>尚且不足...
- else:
    Not enough <color=\#FF2424>Thoughts</color> collected yet...
}

-> END
