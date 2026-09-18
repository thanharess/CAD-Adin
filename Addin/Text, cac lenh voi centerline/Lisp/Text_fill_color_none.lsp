(defun c:TextFillnone ( / ss i ent obj)
  (setq ss (ssget "X" '((0 . "MTEXT")))) ; Lấy tất cả đối tượng MTEXT
  (if ss
    (progn
      (setq i 0)
      (while (< i (sslength ss))
        (setq ent (ssname ss i))
        (setq obj (vlax-ename->vla-object ent))
        (if (= (vla-get-BackgroundFill obj) :vlax-true)
          (progn
            (vla-put-BackgroundFill obj :vlax-false) ; Tắt fill
            (vla-put-UseBackgroundColor obj :vlax-false) ; Tắt màu nền
          )
        )
        (setq i (1+ i))
      )
      (princ "\nĐã tắt fill color cho tất cả MTEXT.")
    )
    (princ "\nKhông tìm thấy đối tượng MTEXT nào.")
  )
  (princ)
)
