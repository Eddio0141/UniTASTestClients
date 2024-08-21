f = movie.frame_advance

-- LegacyInputSystemTest
for i = 1, 5 do
  key.hold("space")
  f(i)
  key.release("space")
  f(i)
end

-- SceneTest
f(100)

-- UGuiTest
-- TODO: once game resolution is deterministic, set this to center of screen
mouse.move(0, 0)
f()

for i = 1, 5 do
  mouse.left()
  f(i)
  mouse.left(false)
  f(i)
end

f(10)
