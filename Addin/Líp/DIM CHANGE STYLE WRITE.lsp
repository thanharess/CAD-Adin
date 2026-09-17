(defun c:DIMCHANGESTYLEWRITE ( / ss dimsty i ent)
  (setq dimsty (getstring T "\nNhập tên Dim Style cần đổi: "))

  ;; kiểm tra dim style có tồn tại không
  (if (not (tblsearch "DIMSTYLE" dimsty))
    (progn
      (alert "Dim Style không tồn tại!")
      (exit)
    )
  )

  ;; chọn DIM trong vùng chọn
  (setq ss (ssget '((0 . "DIMENSION"))))

  (if ss
    (progn
      (setq i 0)
      (repeat (sslength ss)
        (setq ent (ssname ss i))
        (entmod
          (subst
            (cons 3 dimsty)
            (assoc 3 (entget ent))
            (entget ent)
          )
        )
        (setq i (1+ i))
      )
      (command "_.regen")
      (princ "\nĐã đổi Dim Style cho các DIM được chọn.")
    )
    (princ "\nKhông chọn DIM nào.")
  )
  (princ)
)
