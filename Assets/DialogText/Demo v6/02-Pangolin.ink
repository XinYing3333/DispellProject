INCLUDE globals.ink

// EXTERNAL playEmote(emoteName)
#layout:layout1
{ lang == "zh":
#speaker:鄔詩
- else:
#speaker:Wushi
}
#audio:default
//------------------------------
{ lang == "zh":
    ——！？
- else:
    —!? 
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2 #audio:god
//-------------------------------
{ lang == "zh":
    居然有<color=\#FF2424>人類靈媒</color>在。
- else:
    Can't believe there’s a <color=\#FF2424>Human Medium</color> here.
}
{ lang == "zh":
    真會挑時間。
- else:
    Talk about perfect timing.
}


{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 
//-------------------------------
{ lang == "zh":
    這裏的<color=\#FF2424>混亂念頭</color>太過强勢...
- else:
    The <color=\#FF2424>Chaotic Thoughts</color> here are way too powerful...
}
{ lang == "zh":
    正好可以借他的能力一用！
- else:
    We might just have to borrow his power! 
}

#layout:layout1
{ lang == "zh":
#speaker:鄔詩
- else:
#speaker:Wushi
}
#portrait:wushi-shock #audio:wushi
//------------------------------
{ lang == "zh":
    什麽？？？
- else:
    Wait, what???
}
-> END
