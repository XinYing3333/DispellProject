VAR lang = "en"

// EXTERNAL playEmote(emoteName)

{ lang == "zh":
#speaker:鄔詩
    「——！？」#speaker:鄔詩
- else:
#speaker:Wushi
    —!? 
}
// ------------------------------------------------
#speaker:??? #portrait:god-1-black #layout:layout2 #audio:god
{ lang == "zh":
    「恐怕衹能出此下策……」
- else:
    We may have no choice... 
}
#portrait:god-2-black
{ lang == "zh":
    「那我全部吞掉了。」
- else:
    Then I'll swallow it all. 
}
#portrait:god-1-black
{ lang == "zh":
    「等等，好像有人的氣息！住手——」
- else:
    Wait— I sense someone! Stop— 
}
// ------------------------------------------------
#portrait:wushi-palm #layout:layout1 #audio:wushi
{ lang == "zh":
    「哎呦！」#speaker:鄔詩
- else:
    Ouch! #speaker:Wushi
}
#portrait:wushi-scorn
{ lang == "zh":
    「到底是誰在……」
- else:
    Who's there...?
}
// ------------------------------------------------
？#speaker:??? #portrait:god-1 #layout:layout2 #audio:default
？#portrait:god-2
#portrait:wushi-scorn
{ lang == "zh":
    ？？#speaker:鄔詩
- else:
    ？？#speaker:Wushi
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-1
#audio:god
{ lang == "zh":
    「居然有<color=\#FF2424>人類靈媒</color>在，來的正好。」
- else:
    A <color=\#FF2424>human medium</color> is here, perfect timing.
}

//~playEmote("pangolin-WALK")

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1
#layout:layout1
{ lang == "zh":
    「唉!差一點釀成大錯，果然直接吞噬太過冒進。」
- else:
    Ah! That was close. Swallowing it directly was too reckless.
}
{ lang == "zh":
    「凭我的反應速度不會有事的。」
- else:
    With my reaction speed, it should be fine.
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2
{ lang == "zh":
    「應該要把她趕出去，再試一次。」
- else:
    We should drive her out and try again.
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1
{ lang == "zh":
    「不可再冒險，這樣<color=\#FF2424>容易波及現實世界</color>……」
- else:
    No more risks. This could <color=\#FF2424>affect the real world</color>...
}

// ------------------------------------------------

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-scorn
#audio:wushi
{ lang == "zh":
    「...喂！」
- else:
    ...Hey!
}

{ lang == "zh":
    #speaker:白、黑
- else:
    #speaker:White, Black
}
#portrait:god-1
#audio:default
？

{ lang == "zh":
    #speaker:鄔詩
- else:
    #speaker:Wushi
}
#portrait:wushi-palm
#audio:wushi
{ lang == "zh":
    「你們是什麼東西？」
- else:
    What are you?
}

#portrait:wushi-default
{ lang == "zh":
    「這異常空間是你們搞出來的嗎？」
- else:
    Did you create this anomalous space?
}

{ lang == "zh":
    #speaker:白、黑
- else:
    #speaker:White, Black
}
#portrait:god-1
#audio:god
{ lang == "zh":
    「老實說…」
- else:
    To be honest...
}

{ lang == "zh":
    #speaker:黑
- else:
    #speaker:Black
}
#portrait:god-1
{ lang == "zh":
    「老夫不記得了。」
- else:
    I don't remember.
}

{ lang == "zh":
    #speaker:白
- else:
    #speaker:White
}
#portrait:god-2
{ lang == "zh":
    「我也不記得了。」
- else:
    I don't remember either.
}
{ lang == "zh":
    「是啊，説起來到底發生了什麽呢？」
- else:
    Yes, come to think of it, what actually happened?
}
{ lang == "zh":
    「突然閒，身體就變成七零八落了~」
- else:
    I was suddenly bored, and my body fell apart~
}

{ lang == "zh":
    「...就和你說過馬路不要滑手機了。」
- else:
    ...Like I told you, don't use your phone while crossing the street.
}

{ lang == "zh":
    「你什麽時候説過了，而且我連手機都沒有。」
- else:
    When did you ever say that? I don't even have a phone.
}

#portrait:wushi-scorn
#audio:default
{ lang == "zh":
    #speaker:鄔詩
    「（這兩個傢伙肯定跟異常有關，再觀察一下。）」 
- else:
    #speaker:Wushi
    (These two are definitely related to the anomaly. I should observe them further.)
}


#portrait:god-1
#audio:god
{ lang == "zh":
    #speaker:黑
    「小姑娘，別太擔心。」
- else:
    #speaker:Black
    Little girl, don't worry.
}
{ lang == "zh":
    「雖事態未明，但是老夫現在有了些 <color=\#FF2424>新的想法</color>。」
- else:
    Although the situation is unclear, I now have some <color=\#FF2424>new ideas</color>.
}

-> END
