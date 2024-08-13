f = movie.frame_advance

-- LegacyInputSystemTest
for i = 1, 5 do
    key.hold("space")
    f(i)
end

-- SceneTest
f(100)

-- UGuiTest
mouse.move(960, 540)
f()

for i = 1, 5 do
    mouse.left()
    f(i)
    mouse.left(false)
    f(i)
end

f(10)
