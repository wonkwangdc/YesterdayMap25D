# 주인공 콘셉트 1 — 좀비 아포칼립스 덕후·DIY 벙커 제작자

## 디자인 의도

- 30대 초반의 평범한 한국인 생존자
- 좀비 아포칼립스 콘텐츠를 좋아해 집 아래에 DIY 벙커를 만든 준비형 덕후
- 군인이나 영웅보다는 영리하고 약간 피곤해 보이는 생활형 인물
- 안경, 손으로 그린 지도, 실용적인 배낭으로 덕후·준비성을 표현
- 짙은 청회색과 올리브색을 중심으로 사용하고 주황색 비상 장비를 강조색으로 사용

## 애니메이션 시트 규격

- 4방향 × 방향별 4프레임 = 총 16프레임
- 행: 아래 보기 / 왼쪽 보기 / 오른쪽 보기 / 위 보기
- 열: 왼발 접지 / 통과 자세 / 오른발 접지 / 반대 통과 자세
- Unity용 시트: 1252 × 1252 px
- 셀: 313 × 313 px
- 배경: 투명

## 최종 콘셉트 생성 프롬프트

> Create a polished 2D game character concept sheet for a Korean male protagonist in a rainy urban survival game titled "어제의 지도". Character concept: early 30s, an ordinary office-worker-type hobbyist who loves zombie apocalypse fiction and secretly built a practical DIY bunker beneath his home before the disaster. He must look clever, prepared, slightly sleep-deprived, earnest, and a little nerdy—not a soldier, not a superhero. Outfit: dark blue-gray weatherproof hooded jacket over a muted teal hoodie, compact charcoal utility vest, faded olive cargo pants, worn waterproof hiking boots, fingerless work gloves, small practical backpack, orange emergency whistle and amber flashlight clipped to a strap, folded homemade bunker map peeking from a pocket. No gun, no large weapon, no gore. Korean facial features, short slightly messy black hair, subtle black rectangular glasses, slim-to-average build. Visual language: clean stylized 2D indie-game character art, readable silhouette, restrained dark blue-gray/olive palette with small amber safety accents, soft cel shading, suitable as the visual anchor for a 2.5D top-down Unity game. Show one consistent character in three full-body views: front, three-quarter, and back, evenly spaced on a plain neutral light gray background. Include a small separate close-up of the backpack/gear only. No written labels, no logos, no watermark, no UI, no scene background.

## 최종 보행 시트 생성 프롬프트

> Using the referenced character concept as the strict identity and costume reference, create a production-ready 2D top-down/three-quarter character WALK sprite sheet for a Unity 2.5D game. EXACTLY 16 character sprites arranged in an exact 4 columns by 4 rows matrix, with no missing or extra characters.
>
> ROW ORDER, top to bottom: Row 1: facing DOWN / toward camera. Row 2: facing LEFT. Row 3: facing RIGHT. Row 4: facing UP / away from camera.
>
> COLUMN ORDER for every row, left to right: Frame 1: left-foot contact. Frame 2: passing pose. Frame 3: right-foot contact. Frame 4: opposite passing pose.
>
> Keep the exact same Korean male character: short messy black hair, subtle rectangular glasses, dark blue-gray hooded rain jacket over muted teal hoodie, charcoal utility vest, faded olive cargo pants, worn hiking boots, fingerless gloves, compact black backpack, small orange emergency whistle and amber flashlight. No gun, no weapon. Simplify details into clean stylized indie-game sprite art with crisp outlines, restrained cel shading, strong readable silhouette, and consistent proportions. The character should be about 220–260 pixels tall within each conceptual cell, large enough for a 2.5D top-down game, not pixel art. Maintain identical scale, center position, foot baseline, lighting, camera angle, and costume across all 16 cells. Side-facing rows must be true profiles, not front poses. Back row must clearly show the backpack.
>
> Canvas is a perfect square. Background must be a perfectly uniform, solid, flat chroma-key magenta #FF00FF with absolutely no texture, gradient, shadow, floor, glow, grid lines, borders, cell dividers, text, numbers, labels, UI, logo, or watermark. Leave generous magenta padding around every sprite so cells can be sliced evenly. Do not let any sprite cross a cell boundary.

## 제작 방식

OpenAI 내장 이미지 생성으로 콘셉트 원화를 만든 뒤 이를 참조 이미지로 사용해 4 × 4 보행 시트를 생성했습니다. 마젠타 크로마키를 투명화하고, Unity에서 균등하게 자를 수 있도록 4의 배수 크기로 중앙 정리한 뒤 16개 개별 PNG로 분할했습니다.
