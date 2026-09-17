;;; BM_Rename.lsp
;;; Chức năng: Đổi tên block
;;; Cách dùng: APPLOAD, gõ BLRENAME

(defun bm-print (s) (princ (strcat "\n" s)))

(defun bm-get-block-name (/ ent ename edata blkname)
  (setq ent (entsel "\nChọn một block (INSERT): "))
  (if (not ent)
    (progn (bm-print "❌ Bạn chưa chọn block. Thoát.") (exit))
  )
  (setq ename (car ent))
  (setq edata (entget ename))
  (setq blkname (cdr (assoc 2 edata)))
  (if (not blkname)
    (progn (bm-print "❌ Không xác định được tên block.") (exit))
  )
  blkname
)

(defun bm-rename-block (old new / exists)
  (if (or (not new) (= new ""))
    (bm-print "Tên mới không hợp lệ.")
    (progn
      (setq exists (tblsearch "BLOCK" new))
      (if exists
        (progn
          (bm-print (strcat "Có block tên \"" new "\" đã tồn tại. Hủy."))
        )
        (progn
          (command "_.-rename" "_block" old new)
          (bm-print (strcat "✅ Đã đổi tên block \"" old "\" -> \"" new "\"."))
        )
      )
    )
  )
)

(defun c:BLRENAME (/ blkname newname)
  (princ "\n=== BM_Rename: Đổi tên block ===")
  (setq blkname (bm-get-block-name))
  (bm-print (strcat "🔹 Block được chọn: " blkname))
  (setq newname (getstring T (strcat "\nNhập tên mới cho block \"" blkname "\": ")))
  (bm-rename-block blkname newname)
  (princ)
)

(princ "\nGõ BLRENAME để chạy lệnh.")
(princ)