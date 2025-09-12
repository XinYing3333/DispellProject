INCLUDE globals.ink
EXTERNAL playEmote(emoteName)

hi#speaker:NPC1  #layout:layout1 #audio:default
this is a <b><color=\#FF1E35>test</color>.
    -> sub
    
=== sub ===
~ playEmote("pangolin-walk")
choose a number #portrait:change 

  + [1]
    -> chosen("1")
      + [2]
    -> chosen("2")
   

=== chosen(number) ===
~ playEmote("pangolin-ball02")
~ chooseNumber = number
ok you chose {number}.

->END