(defun c:BLsaveasnewblock ( / ent blkName newBlkName insPt ss copyEnt)
  ;; Chọn block gốc
  (setq ent (entsel "\nChọn block cần copy: "))
  (if ent
    (progn
      ;; Lấy tên block gốc
      (setq blkName (cdr (assoc 2 (entget (car ent)))))

      ;; Nhập tên mới cho block
      (setq newBlkName (getstring T "\nNhập tên block mới: "))

      ;; Chọn điểm chèn cho block mới
      (setq insPt (getpoint "\nChọn điểm chèn block mới: "))

      ;; Copy block gốc ra vị trí mới
      (command "_.COPY" (car ent) "" '(0 0 0) insPt)

      ;; Lấy đối tượng vừa copy (Last)
      (setq copyEnt (entlast))

      ;; Explode bản sao
      (command "_.EXPLODE" copyEnt)

      ;; Lấy các đối tượng vừa explode
      (setq ss (ssget "L"))

      ;; Tạo block mới từ các đối tượng đó
      (command "_.-BLOCK" newBlkName insPt ss "")

      ;; Chèn block mới
      (command "_.INSERT" newBlkName insPt 1.0 1.0 0.0)
    )
    (princ "\nKhông chọn block nào.")
  )
  (princ)
)
