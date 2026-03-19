INCLUDE globals.ink
// EXTERNAL playEmote(emoteName)

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:god
{ lang == "zh":
    「必須把它<color=\#FF2424>擊暈</color>后才能吸收。」
- else:
    You must <color=\#FF2424>stun</color> it before you can absorb it.
}
{ lang == "zh":
    「靈媒，注意不要被傷到！」
- else:
    Medium, be careful not to get hurt!
}

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
} 
#portrait:wushi-shock #layout:layout1 #audio:wushi
{ lang == "zh":
    「我從來沒有聽說過念頭會攻擊人啊？」
- else:
    I’ve never heard of Thoughts attacking people.
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1 #audio:god
{ lang == "zh":
    「這裡畢竟祂們形成的空間，凡世經驗未必適用。」
- else:
    This is, after all, a space formed by them. Mortal experience may not apply here.
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2
{ lang == "zh":
    「好消息是，在這裡頭你也死不了。」
- else:
    The good news is, you can’t die in here.
}

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-scorn #audio:wushi
{ lang == "zh":
    「這算是好消息嗎？」
- else:
    Is that supposed to be good news?
}

-> END
