;;; BM_Explode.lsp
;;; Chức năng: Phá khối (explode) tất cả block trùng tên
;;; Cách dùng: APPLOAD, gõ BLEXPLODE
;;; Sửa lỗi: Mặc định áp dụng cho tất cả instances, đảm bảo explode toàn bộ ss.

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

(defun bm-get-all-inserts-by-name (name / ss)
  (ssget "X" (list (cons 0 "INSERT") (cons 2 name)))
)

(defun bm-explode-or-erase-or-zoom (opt blkname / ss cnt i en)
  (setq ss (bm-get-all-inserts-by-name blkname))
  (if (not ss)
    (bm-print "❌ Không tìm thấy instance nào để thao tác.")
    (progn
      (setq cnt (sslength ss))
      (cond
        ((= opt "explode")
          (setq i 0)
          (repeat cnt
            (setq en (ssname ss i))
            (command "_.explode" en "")
            (setq i (1+ i))
          )
          (bm-print (strcat "💥 Đã phá khối " (itoa cnt) " instance(s) của \"" blkname "\"."))
        )
      )
    )
  )
)

(defun c:BLEXPLODE (/ blkname)
  (princ "\n=== BM_Explode: Phá khối block ===")
  (setq blkname (bm-get-block-name))
  (bm-print (strcat "🔹 Block được chọn: " blkname))
  (bm-explode-or-erase-or-zoom "explode" blkname)
  (princ)
)

(princ "\nGõ BLEXPLODE để chạy lệnh.")
(princ)