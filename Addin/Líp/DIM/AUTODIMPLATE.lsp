(defun c:DIMAUTOPLATES (/ ss i ent obj typ verts v1 v2)
  (vl-load-com)
  (setq ss (ssget "_:L" '((0 . "LWPOLYLINE")))) ; chỉ chọn polyline
  (if ss
    (progn
      (repeat (setq i (sslength ss))
        (setq ent (ssname ss (setq i (1- i))))
        (setq obj (vlax-ename->vla-object ent)
              n (fix (vla-get-Coordinates obj))
        )

        ;; Lấy danh sách điểm polyline
        (setq verts '()
              coords (vlax-get obj 'Coordinates)
              cnt 0
        )
        (while (< cnt (length coords))
          (setq verts (append verts (list (list (nth cnt coords) (nth (1+ cnt) coords)))))
          (setq cnt (+ cnt 2))
        )

        ;; DIM từng cạnh
        (setq j 0)
        (repeat (- (length verts) 1)
          (setq v1 (nth j verts)
                v2 (nth (1+ j) verts))
          (command "_.DIMLINEAR" v1 v2 pause)
          (setq j (1+ j))
        )

        ;; Nếu polyline đóng, dim thêm cạnh cuối về đầu
        (if (= (vla-get-Closed obj) :vlax-true)
          (command "_.DIMLINEAR" (last verts) (car verts) pause)
        )
      )
      (princ "\n✓ Đã tạo DIM cho tất cả các cạnh polyline.")
    )
    (princ "\nKhông chọn được polyline.")
  )
  (princ)
)
