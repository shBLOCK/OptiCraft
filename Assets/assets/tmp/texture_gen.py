import pygame
from spatium import *

pygame.init()

img = pygame.Surface(Vec2i(256), flags=pygame.SRCALPHA)
for x in range(256):
    for y in range(256):
        img.set_at((x, y), (0, 255 - y, 0, 0))
pygame.image.save(img, "Resources/y_gradient.png")
