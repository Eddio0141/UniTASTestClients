f = movie.frame_advance

-- LegacyInputSystemTest
for i = 1, 5 do
  if i == 1 then
    key.hold("A")
  elseif i == 3 then
    key.hold("d")
  end
  key.hold("space")
  f()
  key.release("space")
  f()
  if i == 1 then
    key.release("A")
  elseif i == 3 then
    key.release("d")
  end
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
